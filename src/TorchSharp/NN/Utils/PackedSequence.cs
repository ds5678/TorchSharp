// Copyright (c) .NET Foundation and Contributors.  All Rights Reserved.  See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using static TorchSharp.PInvoke.NativeMethods;

namespace TorchSharp
{
    public static partial class torch
    {
        public static partial class nn
        {
            public static partial class utils
            {
                public static partial class rnn
                {
                    /// <summary>
                    /// A packed batch of variable length sequences.
                    /// </summary>
                    public sealed class PackedSequence : IDisposable
                    {
                        internal DisposeScope OwningDisposeScope { get; set; }

                        /// <summary>
                        /// Class wrapping PyTorch's packedsequence object reference.
                        /// </summary>
                        internal sealed class HType : SafeHandle
                        {
                            public HType(IntPtr preexistingHandle, bool ownsHandle)
                                : base(IntPtr.Zero, ownsHandle)
                            {
                                SetHandle(preexistingHandle);
                            }

                            public override bool IsInvalid => handle == IntPtr.Zero;

                            // This is just for marshalling
                            internal HType() : base(IntPtr.Zero, true)
                            {
                            }

                            protected override bool ReleaseHandle()
                            {
                                THSNN_PackedSequence_dispose(handle);
                                handle = IntPtr.Zero;
                                return true;
                            }
                        }

                        /// <summary>
                        /// The packed sequences
                        /// </summary>
                        public readonly Tensor data;

                        /// <summary>
                        /// Batch size at each sequence step
                        /// </summary>
                        public readonly Tensor batch_sizes;

                        /// <summary>
                        /// The sorted indices
                        /// </summary>
                        public readonly Tensor sorted_indices;

                        /// <summary>
                        /// The original indices
                        /// </summary>
                        public readonly Tensor unsorted_indices;
                        /// <summary>
                        /// Is true if the PackedSequence has been disposed, false otherwise.
                        /// </summary>
                        public bool IsInvalid => handle.IsInvalid;
                        private HType handle;

                        internal PackedSequence(HType handle)
                        {
                            this.handle = handle;
                            this.data = new Tensor(THSNN_PackedSequence_data(handle));
                            this.batch_sizes = new Tensor(THSNN_PackedSequence_batch_sizes(handle));
                            this.sorted_indices = new Tensor(THSNN_PackedSequence_sorted_indices(handle));
                            this.unsorted_indices = new Tensor(THSNN_PackedSequence_unsorted_indices(handle));
                            OwningDisposeScope = DisposeScopeManager.ThreadSingleton.RegisterOnCurrentDisposeScope(this.data);
                            OwningDisposeScope = DisposeScopeManager.ThreadSingleton.RegisterOnCurrentDisposeScope(this.batch_sizes);
                            OwningDisposeScope = DisposeScopeManager.ThreadSingleton.RegisterOnCurrentDisposeScope(this.sorted_indices);
                            OwningDisposeScope = DisposeScopeManager.ThreadSingleton.RegisterOnCurrentDisposeScope(this.unsorted_indices);
                            OwningDisposeScope = DisposeScopeManager.ThreadSingleton.RegisterOnCurrentDisposeScope(this);
                        }

                        internal HType Handle => handle;

                        /// <summary>
                        ///   Releases the storage.
                        /// </summary>
                        public void Dispose()
                        {
                            this.data.Dispose();
                            this.batch_sizes.Dispose();
                            this.sorted_indices.Dispose();
                            this.unsorted_indices.Dispose();
                            OwningDisposeScope?.MarkAsDisposed(this);

                            if (handle != null && !handle.IsInvalid) {
                                handle.Dispose();
                                handle.SetHandleAsInvalid();

                            }
                        }
                        /// <summary>
                        /// Moves PackedSequence to the outer DisposeScope. If there is no outer DisposeScope, it's detached from the
                        /// DisposeScope system.
                        /// </summary>
                        /// <returns>The same PackedSequence that the method was called on</returns>
                        public PackedSequence MoveToOuterDisposeScope()
                        {
                            OwningDisposeScope?.MoveToOuter(this.data);
                            OwningDisposeScope?.MoveToOuter(this.batch_sizes);
                            OwningDisposeScope?.MoveToOuter(this.sorted_indices);
                            OwningDisposeScope?.MoveToOuter(this.unsorted_indices);
                            OwningDisposeScope?.MoveToOuter(this);
                            return this;
                        }

                        /// <summary>
                        /// Detaches the PackedSequence completely from the DisposeScope system.
                        /// </summary>
                        /// <returns>The same PackedSequence that the method was called on</returns>
                        public PackedSequence DetachFromDisposeScope()
                        {
                            OwningDisposeScope?.Detach(this.data);
                            OwningDisposeScope?.Detach(this.batch_sizes);
                            OwningDisposeScope?.Detach(this.sorted_indices);
                            OwningDisposeScope?.Detach(this.unsorted_indices);
                            OwningDisposeScope?.Detach(this);
                            return this;
                        }

                        public PackedSequence MoveToOtherDisposeScope(PackedSequence other)
                        {
                            return MoveToOtherDisposeScope(other.OwningDisposeScope);
                        }

                        public PackedSequence MoveToOtherDisposeScope(DisposeScope other)
                        {
                            if (OwningDisposeScope == null && other != null) {
                                other.Attach(this.data);
                                other.Attach(this.batch_sizes);
                                other.Attach(this.sorted_indices);
                                other.Attach(this.unsorted_indices);
                                other.Attach(this);
                            }
                            else {
                                OwningDisposeScope?.MoveToOther(other, this.data);
                                OwningDisposeScope?.MoveToOther(other, this.batch_sizes);
                                OwningDisposeScope?.MoveToOther(other, this.sorted_indices);
                                OwningDisposeScope?.MoveToOther(other, this.unsorted_indices);
                                OwningDisposeScope?.MoveToOther(other, this);
                            }
                            return this;
                        }
                    }
                }
            }
        }
    }
}