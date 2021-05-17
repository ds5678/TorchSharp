// Copyright (c) Microsoft Corporation and contributors.  All Rights Reserved.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Runtime.Serialization;

using System.IO.Compression;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Collections.Generic;
using System.Diagnostics;
using TorchSharp.Tensor;
using TorchSharp.NN;

using static TorchSharp.NN.Modules;
using static TorchSharp.NN.Functions;
using static TorchSharp.Tensor.TensorExtensionMethods;

namespace TorchSharp.Examples
{
    /// <summary>
    /// FGSM Attack
    ///
    /// Based on : https://pytorch.org/tutorials/beginner/fgsm_tutorial.html
    /// </summary>
    /// <remarks>
    /// There are at least two interesting data sets to use with this example:
    /// 
    /// 1. The classic MNIST set of 60000 images of handwritten digits.
    ///
    ///     It is available at: http://yann.lecun.com/exdb/mnist/
    ///     
    /// 2. The 'fashion-mnist' data set, which has the exact same file names and format as MNIST, but is a harder
    ///    data set to train on. It's just as large as MNIST, and has the same 60/10 split of training and test
    ///    data.
    ///    It is available at: https://github.com/zalandoresearch/fashion-mnist/tree/master/data/fashion
    ///
    /// In each case, there are four .gz files to download. Place them in a folder and then point the '_dataLocation'
    /// constant below at the folder location.
    ///
    /// The example is based on the PyTorch tutorial, but the results from attacking the model are very different from
    /// what the tutorial article notes, at least on the machine where it was developed. There is an order-of-magnitude lower
    /// drop-off in accuracy in this version. That said, when running the PyTorch tutorial on the same machine, the
    /// accuracy trajectories are the same between .NET and Python. If the base convulutational model is trained
    /// using Python, and then used for the FGSM attack in both .NET and Python, the drop-off trajectories are extremenly
    /// close.
    /// </remarks>
    public class AdversarialExampleGeneration
    {
        private readonly static string _dataLocation = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "..", "Downloads", "mnist");

        private static int _epochs = 4;
        private static int _trainBatchSize = 64;
        private static int _testBatchSize = 128;

        static void Main(string[] args)
        {
            var cwd = Environment.CurrentDirectory;

            var dataset = args.Length > 0 ? args[0] : "mnist";
            var datasetPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "..", "Downloads", dataset);

            Torch.SetSeed(1);

            //var device = Device.CPU;
            var device = Torch.IsCudaAvailable() ? Device.CUDA : Device.CPU;
            Console.WriteLine($"\n  Running AdversarialExampleGeneration on {device.Type.ToString()}\n");
            Console.WriteLine($"Dataset: {dataset}");

            if (device.Type == DeviceType.CUDA) {
                _trainBatchSize *= 4;
                _testBatchSize *= 4;
                _epochs *= 4;
            }

            var sourceDir = _dataLocation;
            var targetDir = Path.Combine(_dataLocation, "test_data");

            if (!Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
                Utils.Decompress.DecompressGZipFile(Path.Combine(sourceDir, "train-images-idx3-ubyte.gz"), targetDir);
                Utils.Decompress.DecompressGZipFile(Path.Combine(sourceDir, "train-labels-idx1-ubyte.gz"), targetDir);
                Utils.Decompress.DecompressGZipFile(Path.Combine(sourceDir, "t10k-images-idx3-ubyte.gz"), targetDir);
                Utils.Decompress.DecompressGZipFile(Path.Combine(sourceDir, "t10k-labels-idx1-ubyte.gz"), targetDir);
            }

            MNIST.Model model = null;

            var normImage = TorchVision.Transforms.Normalize(new double[] { 0.1307 }, new double[] { 0.3081 }, device: device);

            using (var test = new MNISTReader(targetDir, "t10k", _testBatchSize, device: device, transform: normImage)) {

                var modelFile = dataset + ".model.bin";

                if (!File.Exists(modelFile)) {
                    // We need the model to be trained first, because we want to start with a trained model.
                    Console.WriteLine($"\n  Running MNIST on {device.Type.ToString()} in order to pre-train the model.");

                    model = new MNIST.Model("model", device);

                    using (var train = new MNISTReader(targetDir, "train", _trainBatchSize, device: device, shuffle: true, transform: normImage)) {
                        MNIST.TrainingLoop(dataset, device, model, train, test);
                    }

                    Console.WriteLine("Moving on to the Adversarial model.\n");

                } else {
                    model = new MNIST.Model("model", Device.CPU);
                    model.load(modelFile);
                }

                model.to(device);
                model.Eval();

                var epsilons = new double[] { 0, 0.05, 0.1, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45, 0.50 };

                foreach (var ε in epsilons) {
                    var attacked = Test(model, nll_loss(), ε, test, test.Size);
                    Console.WriteLine($"Epsilon: {ε:F2}, accuracy: {attacked:P2}");
                }
            }
        }

        private static TorchTensor Attack(TorchTensor image, double ε, TorchTensor data_grad)
        {
            using (var sign = data_grad.sign()) {
                var perturbed = (image + ε * sign).clamp(0.0, 1.0);
                return perturbed;
            }
        }

        private static double Test(
            MNIST.Model model,
            Loss criterion,
            double ε,
            IEnumerable<(TorchTensor, TorchTensor)> dataLoader,
            long size)
        {
            int correct = 0;

            foreach (var (data, target) in dataLoader) {

                data.requires_grad = true;

                using (var output = model.forward(data))
                using (var loss = criterion(output, target)) {

                    model.ZeroGrad();
                    loss.backward();

                    var perturbed = Attack(data, ε, data.grad());

                    using (var final = model.forward(perturbed)) {

                        correct += final.argmax(1).eq(target).sum().ToInt32();
                    }
                }


                GC.Collect();
            }

            return (double)correct / size;
        }
    }
}
