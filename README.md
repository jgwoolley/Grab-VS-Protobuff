# GrabProtobuff
TODO: Map piece missing!!!! Vintagestory.GameContent.MapPiece 
			 * message MapPiece {
  required bytes pixels = 1;      // The actual color data for the map pixels
  optional int32 version = 2;     // Format version (to handle updates)
  optional int64 lastModified = 3; // Timestamp of the last update to this piece
```bash
dotnet run --project ./GrabProtobuff
```

```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
