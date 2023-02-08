# Loupedeck VJOY Plugin

This module is a simple plugin for LoupeDeck that adds virtual jostick output capability via VJOY.
For details on VJOY, refer to these projects:

https://github.com/jshafer817/vJoy

https://github.com/shauleiz/vJoy

http://vjoystick.sourceforge.net


## Usage

Install the plugin in the LoupeDeck plugin folder, usually:

`%localappdata%\Loupedeck\Plugins`

## Functionality


I wrote this because it is not currently possible for the base Loupedeck software to send complex keyboard commands to simulators and other games that use extended scan codes - such as those that distinguish between left and right shift states (shift, alt, ctrl) and other keys like the difference between numpad home and extended home keys.   I also needed something to work with my joystick integration software, Gremlin that ties the Loupedeck to my other inputs and lets me control complex states centrally.  

The excellent WhiteMagic Gremlin controller integrator can be found here: https://github.com/WhiteMagic/JoystickGremlin


Once the plugin in enabled in LoupeDeck, a pair of new commands area available:

### Button Press

This presses a VJOY button when the LoupeDeck button is pressed, which includes the push-down functionaliy of the encoders.

The command takes a single parameter string, encoded as follows:

D#[T/R/P]B#P#

Example: `D4B12`  sends a button 12 pulse on device 4

`D4` - outputs to VJoy device 4  (device number is 1 based so vjoy #1 is 1)

`B01` - pulses button 1 for a default duration of 250 ms

`PB01` - sends and holds button 1

`RB1` - releases button 1

`TB1` - toggles buton 1 state - if on, turns off, if off, turns on - useful in some simulators

`P500` - pulse duration in milliseconds (only use with a regular button pulse) - in this case 500 miliseconds or half a second


### Encoder 

D#L#R#A#P#

Example: `D3L3FL4R5FR6A25`


D3 - outputs to device 3 (device number is 1 based so vjoy #1 is 1)

L3 - pulses button 3 when encoder is rotated left (counter clockwise) slow rotation
FL4 - pulses button 4 when encoder is rotated left (counter clockwise) fast rotation
R5 - pulses button 5 when encoder is rotated right (clockwise) slow rotation
RL6 - pulses button 6 when encoder is rotated right (clockwise) fast rotation
A25 - pulses button 25 when the encoder is pressed

Optional:

P500 - sets the pulse delay to 500ms (or whatever number)

## Notes

This is barebones at this point just to get the concept tested.  I'm sure it can be improved and made more robust.

It is important that the DLLs in the plugin folder match the version of Jvoy you are using to avoid DLL load issues.

The push down action on the encoder may not function if you assign a button command to the encoder.

Tested on Windows 10 and Windows 11 x64 only, Loupedeck Live.

