/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using AOT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LeapInternal {

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr Allocate(UInt32 size, eLeapAllocatorType typeHint, IntPtr state);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void Deallocate(IntPtr buffer, IntPtr state);

  public static class MemoryManager {

    /// <summary>
    /// Specifies whether or not a pooling strategy should be used for the
    /// internal MemoryManager.  If enabled, memory will be periodically 
    /// recycled to be used again instead of being deallocated.  
    /// 
    /// An object may be reclaimed from the pool at any time on the 
    /// worker thread.  If you are running into issues where an object
    /// you are working with is being overwritten, consider making a copy,
    /// or turning up the MinPoolSize.
    /// </summary>
    public static bool EnablePooling = true;

    /// <summary>
    /// Specifies how many objects of a specific type need to be in the pool
    /// before they will start to be recycled.  Turning this number up can
    /// help prevent issues where objects you are working with are being
    /// overwritten with new objects.  Turning this number down can reduce
    /// the total memory footprint used by the memory manager.
    /// </summary>
    public static uint MinPoolSize = 64;

    private static ConcurrentDictionary<IntPtr, ActiveMemoryInfo> _activeMemory =
      new ConcurrentDictionary<IntPtr, ActiveMemoryInfo>();
    private static ConcurrentDictionary<PoolKey, Queue<object>> _pooledMemory =
      new ConcurrentDictionary<PoolKey, Queue<object>>();

    [MonoPInvokeCallback(typeof(Allocate))]
    public static IntPtr Pin(UInt32 size, eLeapAllocatorType typeHint, IntPtr state) {
      try {
        //Construct a key to identify the desired allocation
        PoolKey key = new PoolKey() {
          type = typeHint,
          size = size
        };

        //Attempt to find the pool that holds this type of allocation
        Queue<object> pool;
        if (!_pooledMemory.TryGetValue(key, out pool)) {
          //Construct a new pool if none exists yet
          pool = new Queue<object>();
          _pooledMemory[key] = pool;
        }

        //Attempt to get an object from the pool
        object memory;
        if (EnablePooling && pool.Count > MinPoolSize) {
          memory = pool.Dequeue();
        } else {
          //If the pool is empty, we need to construct a new object
          switch (typeHint) {
            default:
            case eLeapAllocatorType.eLeapAllocatorType_Uint8:
              memory = new byte[size];
              break;
            case eLeapAllocatorType.eLeapAllocatorType_Float:
              memory = new float[(size + sizeof(float) - 1) / sizeof(float)];
              break;
          }
        }

        //Pin the object so its address will not change
        GCHandle handle = GCHandle.Alloc(memory, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();

        //Put the information about the newly pinned allocation into the
        //active memory map so it can be retrieved and freed layer.
        _activeMemory.TryAdd(ptr, new ActiveMemoryInfo() {
          handle = handle,
          key = key
        });

        return ptr;
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
      }

      return IntPtr.Zero;
    }

    [MonoPInvokeCallback(typeof(Deallocate))]
    public static void Unpin(IntPtr ptr, IntPtr state) {
      try {
        //Grab the info for the given pointer
        ActiveMemoryInfo info = _activeMemory[ptr];

        //First we return the object back to its pool
        if (EnablePooling) {
          _pooledMemory[info.key].Enqueue(info.handle.Target);
        }

        //Then we remove the pointer from the active memory map
        ActiveMemoryInfo value;

        _activeMemory.TryRemove(ptr, out value);

        //Finally we unpin the memory
        info.handle.Free();
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
      }
    }

    public static object GetPinnedObject(IntPtr ptr) {
      try {
        return _activeMemory[ptr].handle.Target;
      } catch (Exception) { }
      return null;
    }

    private struct PoolKey : IEquatable<PoolKey> {
      public eLeapAllocatorType type;
      public UInt32 size;

      public override int GetHashCode() {
        return (int)type | (int)size << 4;
      }

      public bool Equals(PoolKey other) {
        return type == other.type &&
               size == other.size;
      }

      public override bool Equals(object obj) {
        if (obj is PoolKey) {
          return Equals((PoolKey)obj);
        } else {
          return false;
        }
      }
    }

    private struct ActiveMemoryInfo {
      public GCHandle handle;
      public PoolKey key;
    }
  }
}
