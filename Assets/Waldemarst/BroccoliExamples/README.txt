Broccoli Tree Creator Examples v1.1

[BroccoliTreeFactoryScene]
Contains a basic Broccoli pipeline to begin building vegetation prefabs. Jus select the "Broccoli Tree Factory" GameObject on the Hierarchy Inspector, then on the inspector window click on "Open Tree Editor Window". On the Broccoli Tree Editor Window you can create new preview by selecting "Generate New Preview".

[CatalogShowcase]
Contains a Collection of Tree Prefab created with Broccoli Tree Creator for displaying purpose.

[TerrainScene]
Scene with Broccoli Trees painted on a Unity Terrain. The Terrain instance contains a BroccoTerrainController to control the wind on these instances.

[ModifyPipelineScene]
This scene contains a script to modify a Broccoli Tree Instance properties from another script.

[RuntimeScene]
The script to generate trees by clicking on the sphere surface is on the "SceneController" GameObject. If you are planning to run the example on a standalone build (outside the editor) make sure you add the "Nature/Tree Creator Bark" and "Nature/Tree Creator Leaves" shaders to the "Always Included Shaders" array on the Graphics option of the Project Settings.

[WindScene]
Scene to serve as an example of manipulating wind on BroccoTree instances.

The folder containing the examples (BroccoliExamples) can be safely removed from your project.

Broccoli Tree Creator Documentation can be fount at Assets/Waldemarst/Broccoli/Broccoli Tree Creator vX.X Documentation.pdf
or at
https://docs.google.com/document/d/1Nr6Z808i7X2zMFq8PELezPuSJNP5IvRx9C5lJxZ_Z-A/edit?usp=sharing