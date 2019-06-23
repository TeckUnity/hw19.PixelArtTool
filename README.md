# hw19.PixelArtTool
Pixel art tool for artists to make awesome stuff without having to leave the comfort of the editor :D

## Notes
### Pixel Art asset inspector (.upx?)
* Draw flattened sprite
* Controls to preview animation if multi-frame
* Controls to toggle/preview layers?
* Button to open editor
* Double click asset to open editor (OnOpenAsset attribute)
* Palette picker (update visual on change of palette)
  * Need to determine how to remap if palette entry count differs
  * Alternately palette should always be 256 entries, default to black
  * Remapping/editing can happen in the pixel art window or the palette inspector

### Pixel Editor
* Tools:
  * Brush
  * Line
  * Flood fill/paint bucket
  * Shape?
  * Marquee select
* Buffers:
  * Canvas (current pixels of layer)
  * Draw buffer (pixels to be blitted to canvas on end operation)
  * Tool/overlay (eg. marquee select frame)
* Command pattern for operations? Built-in Undo static methods used for functionality
  * But store custom undo stack for purpose of viewing/exporting replays
  * Subscribe to undoRedoPerformed to modify stack accordingly
    * need to sort out how to determine if undo or redo without access to Event.current

### Palette inspector
* Controls to custom sort palette (by hue, sat, value, etc)?
* Drag reorder?
* Unsure how this should affect the underlying .pal file, if at all

### Misc
* Palette cycling at runtime?
  * If final asset output is a texture, this would likely need to be shader-based
    * Shader takes grayscale source texture and a ramp texture (generated at save/export)
    * Cycling would simply be stepped uv animation on the ramp
  * Possible to have a bespoke renderer for pixel assets? Probably not useful/realistic
    * Pros: pixel doubling/etc, palette cycling
