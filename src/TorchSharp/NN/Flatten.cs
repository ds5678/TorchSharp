// Copyright (c) .NET Foundation and Contributors.  All Rights Reserved.  See LICENSE in the project root for license information.
using System;
using static TorchSharp.torch;
using static TorchSharp.PInvoke.LibTorchSharp;

namespace TorchSharp
{
    using Modules;

    namespace Modules
    {
        /// <summary>
        /// This class is used to represent a flattening of the input tensors.
        /// </summary>
        public sealed class Flatten : ParamLessModule<Tensor, Tensor>
        {
            internal Flatten(long startDim = 1, long endDim = -1) : base(nameof(Flatten))
            {
                _startDim = startDim;
                _endDim = endDim;
            }

            public override Tensor forward(Tensor tensor)
            {
                return tensor.flatten(_startDim, _endDim);
            }

            private long _startDim;
            private long _endDim;
        }
    }

    public static partial class torch
    {
        public static partial class nn
        {
            /// <summary>
            /// Flattens a contiguous range of dims into a tensor. For use with Sequential.
            /// </summary>
            /// <param name="start_dim">First dim to flatten (default = 1).</param>
            /// <param name="end_dim">Last dim to flatten (default = -1).</param>
            /// <returns></returns>
            public static Flatten Flatten(long start_dim = 1, long end_dim = -1)
            {
                return new Flatten(start_dim, end_dim);
            }
        }
    }
}
