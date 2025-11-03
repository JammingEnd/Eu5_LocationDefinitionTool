# EU5 Map Definition Painting tool! 

Over a thousand locations..... heck probably even quintuple that. That is going to be an absolute MEGA task to do manually. luckily i decided to work on a tool that makes this process a lot quicker and easier.

## How to use
Upon starting the app youre prompted with two fields, one for the base game and one for your mod. Use the browse button to open a file explorer and select the root of your project folders.
> DO NOT SWITCH THESE AROUND the app writes to the contents of the MODDED folder \n
> Not selecting the root folders will most likely crash the app when writing \n
> the basegame folder doenst _need_ to be of the basegame, it can also be from another mod you use as reference 

Once both are selected, the program loads the definitions of the basegame folder and your mod into an internal cache, along with the location map image.

On the left you have three buttons. [ Select - Paint - Province ]. 
### Select
Only updates the province info when you click on an colour

### Paint
Enables the paint tools on the right. 
The paint tools are Location info and Pop info. 
The Paint Filters Add or Update the provinces. So when you only paint with Climate for example, it'll only affect the climate property.
For pop info, it updates size for pops with the same [ type - religion - culture ] 

## Install
??? gotta build da thing

## Notes
- Removing an instance in a named_locations/ file whilst they still have data in other files will crash the program.

## Roadblock
- a ping system to dispay recently interacted provinces
- an overlay for filtering provinces ( using the ping system probs)
- QoL like remembering file locations
