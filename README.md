# Snappable Meshes PCG with automatic generation of piece metadata

An implementation of the Snappable Meshes PCG technique in Unity with automatic
generation of piece metadata.

## What is it?

The Snappable Meshes PCG technique consists of a system of connectors with pins
and colors which constrains how any two map pieces (i.e., meshes) can snap
together. Through the visual design and manual specification of these connection
constraints ([v1.0.0]) or a new auto-generation approach ([v2.0.0]), as well as
an easy-to-follow generation procedure, the method is accessible to game
designers and/or other non-experts in PCG, AI or programming.

This repository contains prototype implementations of these techniques developed
in the Unity game engine.

## How does it work?

![01](https://user-images.githubusercontent.com/3018963/127988176-a3002b05-bc4c-4eb1-b817-b6fd955e6b85.png)
![02](https://user-images.githubusercontent.com/3018963/127988173-6c761e64-6e91-464d-b5e0-6449a7ac3978.jpg)
![03](https://user-images.githubusercontent.com/3018963/147121060-0631634a-be54-46e6-8e06-bf867b03a845.png)

## Published research

### v2.0.0 - Automatic generation of map piece metadata

* de Andrade, D. & Fachada, N. (2023). Automated Generation of Map Pieces for
  Snappable Meshes. In Proceedings of Foundations of Digital Games 2023, FDG
  '23, Lisbon, Portugal. ACM. <https://doi.org/10.1145/3582437.3582483>

### v1.0.0 - Original Snappable Meshes implementation

* Fachada, N., e Silva, R.C., de Andrade, D. & Códices, N. (2022). Unity
  Snappable Meshes. Software Impacts, 13, 100363.
  <https://doi.org/10.1016/j.simpa.2022.100363>
* e Silva, R. C., Fachada, N., de Andrade, D., & Códices, N. (2022). Procedural
  Generation of 3D Maps with Snappable Meshes. IEEE Access, 10.
  <https://doi.org/10.1109/ACCESS.2022.3168832>

### v0.0.1 - Preliminary work

* e Silva, R. C., Fachada, N. Códices, N. & de Andrade, D. (2020). Procedural
  Game Level Generation by Joining Geometry with Hand-Placed Connectors. In
  Proceedings of Videojogos 2020 - 12th International Videogame Sciences and
  Arts Conference, VJ '20 (pp. 80-93), Mirandela, Portugal. SPCV.
  <http://videojogos2020.ipb.pt/docs/ProceedingsVJ2020.pdf#page=80>

## Third-party plugins

This software uses the following third-party plugins:

* [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes)
  (MIT License)
* [Array2DEditor](https://github.com/Eldoir/Array2DEditor) (MIT License)

## License

[Apache 2.0](LICENSE)

[v1.0.0]:https://github.com/VideojogosLusofona/snappable-meshes-pcg/releases/tag/v1.0.0
[v2.0.0]:https://github.com/VideojogosLusofona/snappable-meshes-pcg/releases/tag/v2.0.0