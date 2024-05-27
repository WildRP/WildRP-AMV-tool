# Deferred Probes - How To

This guide should help you get started on using the deferred probe baker section of the program.

## Deferred Probe Structure, and how they work

Before you make any probes, it can be very useful to understand how the probes function and what data they contain. These probes are exclusively used for interiors in Red Dead Redemption 2, and can't be used for reflections in exterior YMAPs or similar.

The probes consist of three textures:

1. [texture name]_0
2. [texture name]_1
3. [texture name]_d

The _0 texture contains albedo information in the RGB channels, and ambient occlusion information in the alpha channel.

The _1 texture contains normal map information in the RGB channels, and a window glass mask in the alpha channel.

The _d texture only has a red channel, which contains depth information for the scene. Any pixel that has a value of 1 will be considered part of the sky by the game.

These textures are used in-game to render a dynamic reflection/lighting probe for interiors. Color, depth, AO, and normal information can be combined to create a new texture that changes with the time of day, so that reflections always stay correct even as the lighting changes. The game even renders dynamic lights (candles, lamps, etc) into this texture to keep the lighting correct!

## Step 0: Project setup

In your project settings, make sure you have specified the name of your interior. This is the name of the MLO as defined in your YTYP - the name is used together with the probe GUID to generate the final texture name of the probe. If this is not set, your probes will not work.

## Step 1: Importing a model

Hit the load model button to start importing a model - this has to be a .GLB or GLTF file. Some parts of your model has to follow certain naming conventions to create a correct probe render.

- Any model part that has 'window' in the name will be considered window glass, and be rendered into the window mask. For convenience, this program also considers the sky a window - so if you don't put any glass in your windows, they should render correctly into the scene.
  - IMPORTANT: Don't put the word 'window' in any other model part names - window frames, for example.
- Any model part with 'background' in the name will be excluded from the baked ambient occlusion calculation.
  - Use this for when you're importing landscapes or other large objects outside of your interior. The resolution of the AO bake is dependent on the size of your object, so if you include a very large object in your AO bake, you will get a very low quality AO mask in your probe.
  
Probe positions when exported are relative to the origin of the MLO, so make sure your model is centered properly. Probe *orientations* are fixed in world-space, so currently you have to ensure your models are rotated the same way they are in-world.
  
## Step 2: Set up your probes

Make sure you give every probe a unique GUID by hitting the randomize button. Other than that, you're free to move your probes around wherever you want.

Some important parameters:

- Center offset moves the capture position of your probe camera. This is reflected in-game as well, with how the probe is projected onto objects. It's good practice to put the camera at player height, or a little above, because that's where it is going to be noticed the most.
- Influence extents can help you give a soft transition between probes. It basically scales the probe inside of the bounding box you've set, adding a soft transition to the edge.
  - Example: You have a probe that's 10x10x10 meters. You set the influence extents to 0.9, 0.9, 0.9. The probe will now fully show within 9 meters of the center, and then fade to not applying at all in the last 1 meter.
  
## Step 3: Render and export

Once you hit bake and export, you will be able to find new folders in your Project folder for the probe textures. They will be structured like this:

- [texture name]
- [texture name]_lo
- [texture name]_hi
- [texture_name]_ul

Each folder contains your probe textures at different resolutions. Different resolutions are used in-game depending on the player's graphics settings.

Using CodeX, you will need to create 4 YTD files - each with the name of one of these folders. Add the respective textures to these, and then change their texture type from Texture2D to **TextureCube**. If you don't do this, the texture won't load.

Additionally, you will find a *probe_data.xml* file in the project folder. This contains the XML you need to add to your interior's YTYP. Note that these will **only** work with YTYP files that you convert using CodeX, and not plain-text XML ytyps.

Take each probe XML section, and add it to the appropriate MLO room definition, between the <reflectionProbes></reflectionProbes> tags. If you only have a <reflectionProbes/> tag, go ahead and change it to the correct form before pasting in your probes.

## Step 4: Done

Everything should be working correctly now! This guide will not tell you how to stream assets with RedM, but you should be able to just put your new YTD files alongside your YTYP, and it should work.