# AMV Instructions

Hello! Welcome to the WildRP AMV Tool. This is a barebones instructional guide for the pre-release version of this 
tool.

And yes - there are bugs. Many. I recommend saving your project every time you do anything - and restart the program 
every time you make a new volume inside the same project.

## Usage:
1. Load your model with the load model button. The model has to be in GLTF format - this was the easiest format to 
   load models in using Godot.
2. Create a new volume, and adjust it to the size of your model.
3. Hit the randomize texture name button.
4. Adjust cell counts, and hit bake. Don't go overboard on this - these are 3D textures, and the game needs to load 
   a lot of them. They are pretty low-res in game. The lower you can get away with, the better.
5. Don't forget to save your project!
6. Hit Export Textures.
7. Click on the Open Project Folder.
8. Open up CodeX, and copy out the amv_zone_XX .ytd and .ymt you want to use to your streaming folder.
9. Open the "put_these_in_your_amv_zone.xml" file in notepad or similar.
10. Open your amv_zone ymf, and paste in the new AMV entry from the xml.
11. Add the DDS files to the YTD. **Make sure they are set to Texture3D**!
12. Done! Launch your game and see how it looks. Tweak accordingly.