# Shaman.Streams

## LimitedStream
Wraps a portion of a stream, up to a specified length.
```csharp
using Shaman.Runtime;

var limited = new LimitedStream(stream, length);
// limited.Length == length
```

## UnseekableStreamWrapper
Provides support for `Position` and `Seek` (forward only) for streams that don't support these features.

```csharp
using Shaman.Runtime;

var seekable = new UnseekableStreamWrapper(gzipStream);
seekable.Seek(1024, SeekOrigin.Current);
```

