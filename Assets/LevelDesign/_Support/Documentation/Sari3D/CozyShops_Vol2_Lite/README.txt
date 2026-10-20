COZYSHOPS VOL.2 LITE - 10 FREE BAKERY PROPS
A free sampler from CozyShops Vol.2 - Stylized Bakery
by Sari3D
==================================================


WHAT'S INSIDE
-------------

  10 individual prop prefabs
      6 baked goods: Croissant, Baguette, Pink Donut, Cupcake,
      Chocolate Chip Cookie and Bundt Cake.
      4 kitchen props: Baking Tray, Rolling Pin, Mixing Bowl and
      Wooden Crate.

  A small demo scene
      A baker's corner vignette built only from these 10 props: a tray of
      fresh pastries, a prep station on an upturned crate, and a crate of
      baguettes. The isometric camera (Cam_Isometric) is active; a
      first-person camera (Cam_FirstPerson) is included but disabled -
      enable it to switch.

Every prop is a standalone prefab. Nothing depends on the demo scene.


THIS IS THE FREE VERSION
------------------------

These 10 props are a taster from the full pack:

  CozyShops Vol.2 - Stylized Bakery
  65 props + 4 composite prefabs + 3 room pieces + a furnished demo scene.

The full pack adds 27 more baked goods (breads, pastries, layer cakes,
tarts, pies, macarons and more), the deck oven, display cases, proofing
rack, work table, mixer, scale, wall shelves, canisters and hand tools,
plus four stocked composite units - everything needed to build a complete
bakery.

  Full pack:  https://assetstore.unity.com/packages/slug/410470


TECHNICAL
---------

  Render pipeline    Universal Render Pipeline (URP)
  Unity version      6000.0 LTS and up
  Textures           One 512x512 color atlas (the same atlas as the full pack)
  Materials          1 - M_CozyShops_Bakery (opaque, atlas-based)
  Triangle counts    100 - 768 per prop
  Demo scene         ~13,400 triangles as furnished
  Colliders          Box collider on each prefab root
  Scale              1 unit = 1 meter, real-world proportions
  Scripts            None
  Rigging/animation  None - static props


NAMING
------

  FD_                mesh prefix for food
  SM_ / MD_          mesh prefix by size class (small / medium)
  P_CozyShops_       prefabs
  M_CozyShops_       materials
  T_CozyShops_       textures


NOTES
-----

Everything shares one material.
  All 10 props use the same material and atlas, so they batch well
  together in a scene.

Pivots sit at the base center.
  Props drop straight onto a surface at the right height.

The crate works both ways.
  Upright it holds bread; turned upside down its base becomes a flat
  display top, as in the demo scene's prep station.

Using Lite alongside the full pack.
  The full pack ships its own copy of the atlas and material in its own
  folder. If you own both, the props are identical - you can keep using
  the Lite prefabs or delete the CozyShops_Vol2_Lite folder.


LIGHTING AND POST-PROCESSING
----------------------------

The demo scene uses a realtime directional light with soft shadows and a
Global Volume (VP_CozyShops_Bakery_Lite_Demo: bloom, vignette, neutral
tonemapping). Shadow quality and distance come from your project's URP
asset, not from this pack - if the scene shows no shadows, enable Main
Light shadows in your URP asset.


IF PROPS APPEAR PINK
--------------------

That means the project is not using URP, or the materials need upgrading.
Check that a URP asset is assigned under Project Settings > Graphics, or
use Edit > Rendering > Materials > Convert Selected Built-in Materials to
URP.


SUPPORT
-------

Questions, requests or bug reports:  serhatsari@live.com

If you find these useful, the full pack is one click away - and a rating on
either one genuinely helps.


--------------------------------------------------
CozyShops is a series. Vol.2 ships with its own atlas, and its first
eight color rows are Vol.1's palette carried over exactly - so the bakery
sits next to Vol.1 (Coffee Shop) without a color mismatch, while each
volume works entirely on its own.
