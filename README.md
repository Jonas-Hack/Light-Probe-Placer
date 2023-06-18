# Light Probe Placer
 Unity Editor Tool for Easily Placing Lots of Light Probes

Places LightProbes in an even grid pattern within a rectangular volume, in local space of the LightProbeGroup, while avoiding intersections with scenery.

## Instructions  
- Place script in your project  
- MenuBar -> CustomTools -> Light Probe Placer  
- Select a gameObject with the Light Probe Group Component  
- Move the object to where you want the probes  
- Set size of the volume (in local space)  
- Set probe spacing to an appropriate scale  
**Optionally:**  
- Click avoid Intersection to enable collision checks  
- Margin is the minimum allowed distance to the scene  
- Mask determines which layers to consider  

Click **Generate** to place light probes. Don't worry, *ctrl + z* is supported ;)

## Editor Window  
![editor window image](/demo/EditorWindow.PNG)

## Usage Example
![example scene](/demo/ExampleScene.PNG)