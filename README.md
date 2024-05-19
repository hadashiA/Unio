# Unio

Unio (short for unity native I/O) is a small utility set of I/O using native memory areas.

A drop-in replacement for the `System.IO.File` you are familiar with is provided.

| Feature                         | Description                                              | 
|---------------------------------|----------------------------------------------------------|
| `NativeFile.ReadAllBytes`       | `NativeArray<byte>` version of `File.ReadAllBytes`.      | 
| `NativeFile.ReadAllBytesAsync`  | `NativeArray<byte>` version of `File.ReadAllBytesAsync`. | 
| `NativeFile.WriteAllBytes`      | `NativeArray<byte>` version of `File.WriteAllBytes`.     | 
| `NativeFile.WriteAllBytesAsync` | `NativeArray<byte>` version of `File.WriteAllBytesAsync` | 

In addition, Unio provides NativeArray extensions for interoperability with modern memory-consuming C# APIs.

| Feature                      | Description                                            | 
|:-----------------------------|--------------------------------------------------------|
| `NativeArray<T>.AsMemory()`  | Convert `NativeArray<T>` to `System.Buffers.Memory<T>` |
| `NativeArrayBufferWriter<T>` | `NativeArray<byte>` version of `ArrayBufferWriter<T>`. |

## Performance

Sample of a file of about 1mb read 10 iterations.


|                   | Time    | GC Alloc size (kb) |
|-------------------|---------|--------------------|
| `Unio.NativeFile` | 1.13 ms | 0 kb               |
| `System.IO.File`  | 1.25ms  | 9.240 kb           |


Sample with about 1.7MB serialization in `System.Text.Json` 10 iterations.

|                                    | Time    | GC Alloc size (kb) |
|------------------------------------|---------|--------------------|
| `Unio.NativeArrayBufferWriter`     | 3.26 ms | 204 kb             |
| `System.Buffers.ArrayBufferWriter` | 3.31ms  | 3.000 kb           |


Sample with about 1MB serialization in `Cysharp.MemoryPack` 10 iterations.


|                                    | Time    | GC Alloc size (kb)   |
|------------------------------------|---------|----------------------|
| `Unio.NativeArrayBufferWriter`     | 8.99 ms | 206 kb               |
| `System.Buffers.ArrayBufferWriter` | 8.98ms | 3.560 kb             |



## Motivation

Unity has the ability to allocate native area memory directly, instead of using C#'s GC managed heap.
https://docs.unity3d.com/Manual/JobSystemNativeContainer.html


We want to treat the deserialization result as C# memory, but the raw data before deserialization is not needed on the C# side.

```csharp
using Unio;
using MessagePack-CSharp;

// Unio provides a way to read files into NativeArray<byte>.
using var bytes = NativeFile.ReadAllBytes("/path/to/file");

// NativeMemory can be converted to Memory<byte> using AsMemory().
var deserializedData = MessagePackSerializer.Deserialize<MyData>(bytes.AsMemory());
```

```csharp
using Unio;
using VYaml;

// Unio provides a way to read files into NativeArray<byte>.
using var bytes = NativeFile.ReadAllBytes("/path/to/file");

// Unio provides a way to read files into NativeArray<byte>.
var deserializedData = YamlSerializer.Deserialize<MyData>(bytes.AsMemory());
```

The Unity engine provides an API that directly accepts NativeArray<byte>.
These APIs are useful when dynamic, unspecified texture loading is required, which cannot be pre-built as an asset.

```csharp
var texture = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
texture.LoadRawTextureData(bytes);
```

## Installation

add git URL from Package Manager:

```
https://github.com/hadashiA/Unio.git?path=/Assets/Unio#0.0.1
```

## Usage

### Read file

`NativeFile.ReadAllBytes` / `.ReadAllBytesAsync` is used to read the contents of a file at once into Unity's Native memory area.

```csharp
// Read file with sync
// The return value is a NativeArray<byte>. Dispose when you have finished using it.
using var bytes = NativeFile.ReadAllBytes("/path/to/file");

// Read file with async
// The return value is a Awaitable<NativeArray<byte>>
using var bytes = await NativeFile.ReadAllBytesAsync("/path/to/file", cancellationToken: cancellationToken);
```

Internally, this uses [AsyncReadManager](https://docs.unity3d.com/ScriptReference/Unity.IO.LowLevel.Unsafe.AsyncReadManager.html).
By default, it performs a synchronous wait on the ThreadPool to optimize for latency.

If you wish to change this behavior, you may supply a SynchonizationStrategy argument.

```csharp
using var bytes = await NativeFile.ReadAllBytesAsync("/path/to/file", SynchonizationStrategy.PlayerLoop);
```

- `SynchonizationStrategy.BlockOnThreadPool` (default)
  - Synchronous I/O on thread pools. Lowest latency.
- `SynchonizationStrategy.PlayerLoop`
   - 読み込んだデータを 
   - また、スレッドプールが利用できない環境でも有効な方法である。
   - WebGL環境など、ThreadPoolが利用できない環境ではこちらを設定して下さい。

```

> [!NOTE]
> あああ 



```csharp
// たとえば、MessagePack を読み込んだら
using var bytes = NativeFile.ReadAllBytes("/path/to/file");

// MessagePack-CSharpDeserialize 
var deserializedData = MessagePackSerializer.Deserialize<MyData>(bytes);

// 
var deserializeData = YamlSerializer.Deserialize<MyData>(bytes);
```

### Write file


```csharp
``

```csharp
var textureRawData = texture.GetRawTextureData();
NativeFile.WriteAllBytes


tex.LoadRawTextureData((IntPtr)buffer, (int)textureDataSize);
tex.Apply();
```

- `BlockOnThreadPool` レイテンシは最短になるはずだ。
- `PlayerLoop` ThreadPool が利用できない環境。また単に継続する

```csharp
// Write file 
var bytes = NativeFile.ReadAllBytes("/path/to/file");

// Write file async (only threadpool)
```

### TextAsset as Memory<byte>


```csharp
var textAsset = async Addressable.LoadAssetAsync<TextAsset>(assetPath);

// 
var bytes = textAsset.Data().AsMemory();
```


## LICENSE

MIT

## Author

[@hadashiA](https://github.com/hadashiA)
