# Unio

[![GitHub license](https://img.shields.io/github/license/hadashiA/Unio)](./LICENSE)
![Unity 2021.3+](https://img.shields.io/badge/unity-2021.3+-000.svg)

Unio (short for unity native I/O) is a small utility set of I/O using native memory areas.

It provides a drop-in replacement of the `System.IO.File`.

| Feature                         | Description                                                  | 
|---------------------------------|--------------------------------------------------------------|
| `NativeFile.ReadAllBytes`       | The `NativeArray<byte>` version of `File.ReadAllBytes`.      | 
| `NativeFile.ReadAllBytesAsync`  | The `NativeArray<byte>` version of `File.ReadAllBytesAsync`. | 
| `NativeFile.WriteAllBytes`      | The `NativeArray<byte>` version of `File.WriteAllBytes`.     | 
| `NativeFile.WriteAllBytesAsync` | The `NativeArray<byte>` version of `File.WriteAllBytesAsync` | 

In addition, Unio provides NativeArray extensions for interoperability with modern memory-consuming C# APIs.

| Feature                      | Description                                            | 
|:-----------------------------|--------------------------------------------------------|
| `NativeArray<T>.AsMemory()`  | Convert `NativeArray<T>` to `System.Buffers.Memory<T>` |
| `NativeArrayBufferWriter<T>` | `NativeArray<byte>` version of `ArrayBufferWriter<T>`. |

Motivation:

Unity has the ability to allocate native area memory directly, instead of using C#'s GC managed heap. https://docs.unity3d.com/Manual/JobSystemNativeContainer.html This can load Assets such as Mesh, Texture, and Addressable into its native memory area, but Unio can be used to extend this use.

Overloading the Managed GC area in C# leads to a performance penalty for the game.
- The GC Collect phase stops all managed threads.
- The managed memory area is expanded, the application's maximum memory usage tends to increase.
 
Therefore, it is an important optimization to use the native allocator for memory that does not need to be handled by C#.

For example, serialization/deserialization of dynamic data is a typically example.
Modern serializers (such as [System.Text.Json](https://learn.microsoft.com/dotnet/api/system.text.json), [MessagePack-CSharp](https://github.com/Cysharp/MessagePack-CSharp), [MemoryPack](https://github.com/Cysharp/MemoryPack), [VYaml](https://github.com/hadashiA/VYaml), etc) can take `ReadOnlySequence<byte>` or `IBufferWriter<byte>` as input. Unio is designed to integrate with these.
We want to treat the deserialization result as C# memory, but the raw data before deserialization is not needed on the C# side.

Unio can be used to reduce GC managed heap usage.

![](./docs/gc_bench.png)

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
  - [Read file](#read-file)
  - [Write file](#write-file)
  - [NativeArrayBufferWriter](#native-array-buffer-writer) 
  - [Unity Assets Integration](#unity-assets-integrations)
- [LICENSE](#license)

## Installation

You can use add git URL from Package Manager:

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
// The return value is a `UnityEngine.Awaitable<NativeArray<byte>>`.
using var bytes = await NativeFile.ReadAllBytesAsync("/path/to/file", SynchonizationStrategy.PlayerLoop);
```

- `SynchonizationStrategy.BlockOnThreadPool` (default)
  - Synchronous I/O on thread pools. Lowest latency.
- `SynchonizationStrategy.PlayerLoop`
   - Check for completion every frame by Unity's PlayerLoop.
   - It is suitable for environments where you want to wait on the main thread or where ThreadPool is not available, such as WebGL.


> [!NOTE]
> If you are using Unity less than 2023.2, the async method will use `Task<T>`, not `Awaitalbe<T>`.

In addition, `AsMemory()` can be used to work with modern C# APIs.

```csharp
using var bytes = NativeFile.ReadAllBytes("/path/to/file");

// System.Text.Json
var deserializedData = MessagePackSerializer.Deserialize<MyData>(bytes.AsMemory());

// MessagePack-CSharp
var deserializedData = MessagePackSerializer.Deserialize<MyData>(bytes.AsMemory());

// MemoryPack
var deserializedData = MemoryPackSerializer.Deserialize<MyData>(bytes.AsMemory());

// VYaml
var deserializedData = YamlSerializer.Deserialize<MyData>(bytes.AsMemory());
```

### Write file

```csharp
// Write file 
var bytes = NativeFile.ReadAllBytes("/path/to/file");

// Write file async (only threadpool)
```

### NativeArrayBufferWriter


Serialization example:

```csharp
// Unio provides IBufferWriter<byte> implementation for NativeArray<byte>. 
var bufferWriter = new NativeArrayBufferWriter<byte>(InitialBufferSize);

// System.Text.Json
var jsonWriter = new Utf8JsonWriter(arrayBufferWriter);
JsonSerializer.Serialize(jsonWriter, data, typeof(D), SourceGenerationContext.Default);

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

### Unity Assets Integrations



```csharp
var textAsset = async Addressable.LoadAssetAsync<TextAsset>(assetPath);
var bytes = textAsset.Data().AsMemory();
```

UnityWebRequest

## LICENSE

MIT

## Author

[@hadashiA](https://github.com/hadashiA)
