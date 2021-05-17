// Copyright (c) Microsoft Corporation and contributors.  All Rights Reserved.  See License.txt in the project root for license information.
using System;
using System.Runtime.InteropServices;
using TorchSharp.Tensor;

namespace TorchSharp.NN
{
    /// <summary>
    /// This class is used to represent a ConstantPad2d module.
    /// </summary>
    public class ConstantPad2d : Module
    {
        internal ConstantPad2d (IntPtr handle, IntPtr boxedHandle) : base (handle, boxedHandle) { }

        [DllImport ("LibTorchSharp")]
        private static extern IntPtr THSNN_ConstantPad2d_forward (Module.HType module, IntPtr tensor);

        /// <summary>
        /// Forward pass.
        /// </summary>
        /// <param name="tensor">Input tensor</param>
        /// <returns></returns>
        public override TorchTensor forward (TorchTensor tensor)
        {
            var res = THSNN_ConstantPad2d_forward (handle, tensor.Handle);
            if (res == IntPtr.Zero) { Torch.CheckForErrors(); }
            return new TorchTensor (res);
        }
    }
    public static partial class Modules
    {
        [DllImport ("LibTorchSharp")]
        extern static IntPtr THSNN_ConstantPad2d_ctor (double value, long padding, out IntPtr pBoxedModule);

        /// <summary>
        /// Pads the input tensor using replication of the input boundary.
        /// </summary>
        /// <param name="padding">The size of the padding.</param>
        /// <param name="value"></param>
        /// <returns></returns>
        static public ConstantPad2d ConstantPad2d(long padding, double value)
        {
            var handle = THSNN_ConstantPad2d_ctor(value, padding, out var boxedHandle);
            if (handle == IntPtr.Zero) { Torch.CheckForErrors(); }
            return new ConstantPad2d(handle, boxedHandle);
        }
    }

    public static partial class Functions
    {
        /// <summary>
        /// Pads the input tensor using replication of the input boundary.
        /// </summary>
        /// <param name="x">Input tensor</param>
        /// <param name="padding">The size of the padding: (padding_left , padding_right, padding_top, padding_bottom)</param>
        /// <param name="value"></param>
        /// <returns></returns>
        static public TorchTensor ConstantPad2d (TorchTensor x, long padding, double value)
        {
            using (var d = Modules.ConstantPad2d (padding, value)) {
                return d.forward (x);
            }
        }
    }

}
