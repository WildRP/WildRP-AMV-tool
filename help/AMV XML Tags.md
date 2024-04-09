# XML Tags in AMV files

This is to document what the tags in your AMV descriptor does. The entire descriptor needs to be surrounded by an 
`<Item>` tag to be consumed by CodeX. The example values here are taken from the AMVs for an interior on the WildRP 
server.

- `<enabled value="true" />`
  - Does what it says on the tin - used to toggle off the AMV for testing.
- `<position x="-221.08406" y="802.35974" z="127.69557" />`
  - World-space position of your AMV. Calculated in this app by adding the AMV bounds position to the specified YMAP 
    position.
- `<rotation x="-32" y="0" z="0" />`
  - World space rotation of your AMV. Only the first value is used, and specified in degrees. This program assumes 
    your world YMAP is not rotated.
- `<scale x="3.60185" y="3.7552013" z="4.107205" />`
  - The size of your AMV in world space, divided by two - essentially how much the AMV extends from its origin in 
    any given direction.
- `<falloffScaleMin x="1" y="1" z="1" />` and `<falloffScaleMax x="1.25" y="1.25" z="1.25" />`
  - Describes when the AMV starts blending into other AMVs.
  - `falloffScaleMin` determines where the blend starts, by percentage - 1 in this case means the blend starts at 
    the edge of the AMV. A value lower than 1 means the blend starts inside the AMV. Keep this below 1.
  - `falloffScaleMax` determines the ending edge of the AMV blend - also in a percentage, I believe. The value of 
     1.25 here means that the AMV finishes blending out at 25% of the size of itself outside of the AMV.
- `<samplingOffsetStrength value="0" />`
  - Uncertain, but fairly sure this offsets the sampling position of any surface attempting to sample the AMV - so 
    if you set it to 1, it will sample the AMV 1 meter away from the surface instead of on the surface. Best not to 
    touch this, usually.
- `<falloffPower value="8" />`
  - How sharply this AMV blends into others. 1 is a very soft blend. 16, for example, is a very sharp blend.
- `<distance value="-1" />`
  - Unknown. Set to -1 in every AMV I've seen - I think it determines rendering distance, so you can shut off AMVs 
    that are far away.
- `<cellCountX value="8" />` and corresponding Y and Z values
  - Texture size. Used to sample the AMV texture correctly. Leave this as is.
- `<clipPlane0 x="0" y="0" z="0" w="1" />` to `clipPlane3`
  - Used to cut off the AMV at certain angles or positions. Probably used for things like roofs or other things that 
    might be at weird angles - unknown how to set this properly at the moment. The value seems to be a Quaternion, 
    so it's defined by rotation.
- `<clipPlaneBlend0 value="0" />` to `clipPlaneBlend3`
  - Determines the strength of the above clip planes. Set to 0 for any clip planes that you're not using.
- `<blendingMode>BM_Lerp</blendingMode>`
  - Blending mode for the AMV. Every AMV I've seen is using `BM_Lerp`. Another valid one is `BM_Overwrite`.
- `<layer value="0" />`
  - **Important!** AMVs on the same layer can't blend into each other and instead will switch over with a hard edge. 
    If you are making an interior with multiple AMVs, any volumes that overlap each other need different layers.
  - Higher layers will show on top of lower layers.
  - I usually set my main room one to 0, then any weird shapes on top to 1. Rarely do enough AMVs overlap that I 
    need a third layer.
- `<order value="10" />`
  - Order in the layer. Haven't seen this make a difference yet.
- `<natural value="true" />`
  - I think this essentially means "this AMV represents what this place looks like under natural lighting conditions".
  - In practice, this is another "enabled" switch, I've found. I haven't seen it make a tangible difference anywhere 
    yet - and in direct artificial lighting, the AMVs go away anyway.
- `<attachedToDoor value="false" />`
  - Not sure how to use this - I think it means essentially "turn this off or on as the door it's connected to 
    (world space position-wise?) opens".
  - Presumably used to let natural light in when doors are open.
- `<interior value="true" />`
  - Whether or not this AMV affects interiors.
- `<exterior value="false" />`
  - Whether or not this AMV affects exteriors.
- `<vehicleInterior value="false" />`
  - Whether or not this AMV affects the interior of vehicles?
  - Leave this off, vehicles have their own AMVs that get carried around with them.
- `<sourceFolder>NotRelevant</sourceFolder>`
  - Not used by the game. In the original Rockstar asset database this was used to determine the source of the asset.
  - If CodeX remembered strings we put in here, we could use this to store the AMV name and thus more easily 
    associate YMT entries with particular AMVs. As it is currently, these get hashed and forgotten.
- `<uuid value="1840362054833906944" />`
  - Texture name. Has to be a string of numbers. CodeX converts this into a hexadecimal representation.
- `<iplHash value="0" />`
  - Unknown. Only ever seen it set to 0.
