# Destiny Camera Tool

An app which utilizes PS4 Remote Play to enable smooth camera panning. That's all there is to it.

## Usage

This tool is written specifically with Destiny 2 in mind; there's a mode which hides your weapon by repeatedly swapping weapons in a quick manner when equiping MIDI Mini-Tool or any other weapon that is holstered while held sideways. The timing used can be configured using the lower two bars; the interval in which the button is pressed and how long it is held down before being released again. The values required changes for each setup based on whether or not your using a PS4 Pro, your connection speed and PC hardware specs. **181/38** and **133/33** have worked for most test ran by me. (This mode is optional; the tool can be used without it. Simply skip the appropriate steps.)

* Run the PS4 Remote Play app with a controller plugged into your PC and connect to your PS4.
* Now start this tool.
* Check the first checkbox and your controller is disabled and the controls are handed to the tool
* Check the second checkbox and the triangle-button mashing begins and you can set up any movement you want.
* Once you're happy with the pan, uncheck the first box again
* Position your camera to your starting location
* Equip and swap your active weapon to MIDA Mini-Tool or similar
* Re-check the first box.
* The camera/character should start moving just like you set up and the weapon should vanish from the screen.

Example:  
![example gif](http://i.vimaster.de/direct/krUHCvM.gif)

## Installation

You can either download the source code and compile it using VS2015 Community Edition (or higher) or [download pre-build binaries](https://github.com/ViMaSter/destinycameratool/releases) for Windows.
