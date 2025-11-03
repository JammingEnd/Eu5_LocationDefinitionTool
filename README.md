# EU5 Map Definition Painting tool! 

Over a thousand locations..... heck probably even quintuple that. That is going to be an absolute MEGA task to do manually. luckily i decided to work on a tool that makes this process a lot quicker and easier.

## Pre-usage
the app uses folder names to read and write. 
- the files in common/* are retrieved dynamically, it'll grab the top most file.
- not all folders in common/ need to be present. just make sure at least one of the two directories have an instance. 
- the file for the location-pop definition just requires the word 'pop' (no capitals) to be present in the name. but i'll change this when the game releases.

## How to use
Upon starting the app youre prompted with two fields, one for the base game and one for your mod. Use the browse button to open a file explorer and select the root of your project folders.
> DO NOT SWITCH THESE AROUND the app writes to the contents of the MODDED folder
> 
> Not selecting the root folders will most likely crash the app when writing
> 
> the basegame folder doenst _need_ to be of the basegame, it can also be from another mod you use as reference

> While testing nothing catastrophic occured in the files. crashing doesnt write or delete. i still suggest editing it in a backup folder just in case

Once both are selected, the program loads the definitions of the basegame folder and your mod into an internal cache, along with the location map image.

On the left you have three buttons. [ Select - Paint - Province ]. 
### Select
Only updates the province info when you click on an colour

### Paint
Enables the paint tools on the right. 
The paint tools are Location info and Pop info. 
The Paint Filters Add or Update the provinces. So when you only paint with Climate for example, it'll only affect the climate property.
For pop info, it updates size for pops with the same [ type - religion - culture ] 

### Province
Not available right now.

## Install
Download the Zip, extract it. use the .exe to run

## Notes / Known bugs
- Removing an instance in a named_locations/ file whilst they still have data in other files will crash the program.
- Name Write reverts to placeholder under specific circumstance
- Painting Pop info whilst there no Location info present yields weird results. to fix this. paint the province with any Location info data and write it. selecting it again should present all data

Please contact me on discord or create an Issue for any bugs. Provide me with the steps you took to encounter it and i'll see it fixed.

## Roadmap
- a ping system to dispay recently interacted provinces
- an overlay for filtering provinces ( using the ping system probs)
- Province creation and view 
