# WildRP AMV Tool

This tool was created to help bake **Ambient Mask Volumes** for use in Red Dead Redemption 2 modding. AMVs are used 
by the game to create ambient lighting - they are essentially 3D textures with baked ambient occlusion.

The tool has been written using the Godot engine, which uses a different coordinate system from Red Dead Redemption 
2 - where RDR2 is using Z as up, Godot uses a Y-up coordinate system. Thus, the tool internally uses Godot's 
coordinates, but presents the coordinates to the end user as Z-up.

## License

This repo is licensed under [Creative Commons 4.0 BY-NC-SA](https://creativecommons.org/licenses/by-nc/4.0/) - the TL;DR is that you can make derivative works or use this in other works, as long as you:

- Credit WildRP and me (Andicraft / Andrea Jörgensen)
- Don't sell what you make based on this - this repo is for non-commercial use only.
  - That doesn't mean you can't sell content that uses AMVs that were generated with this program. It just means you can't use the code from this program in commercial applications.

## Dependencies

To compile this project you will need:

- [Godot Engine 4.2.1](https://godotengine.org/download/archive/4.2.1-stable/) .NET version
- [.NET SDK 8.0 or later](https://dotnet.microsoft.com/en-us/download)

## Contributing

- There are many bugs at the moment! Pull requests that fix anything are very welcome.
- If you find a bug that isn't listed in the Issues page, feel free to add a new issue.

## Acknowledgements

- Uses [Markdown Label](https://github.com/daenvil/MarkdownLabel/blob/main/addons/markdownlabel/markdownlabel.gd) 
  for the help page, to convert markdown files into BBCode.
- Thanks to dexyfex, Któs, and CP on the CodeWalker discord for their prior research into AMVs.
- Huge thanks to alexguirre on the same discord, who finally figured out the reflection probe texture naming. 
  (reflection probe feature is on the way!)

This project uses the [Godot Jolt](https://github.com/godot-jolt/godot-jolt) addon for improved physics performance. It includes 
version 0.12.0.
