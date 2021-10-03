## LiveSplit.TOEM

This project is a work-in-progress AutoSplitter for the game TOEM. The current version has not been tested
thoroughly and therefore makes no guarantees as to whether it will work properly.

## Installation

First, you will need to acquire compiled binaries of the AutoSplitter.
You can either compile them yourself (I am using VisualStudio 2017) or download them from the latest release you can find in this repository.
After that you can install the DLL file just like any other LiveSplit component by moving it into the 'Components' folder of your LiveSplit installation.
To activate the Autosplitter all you need to do is add it to your LiveSplit layout - the component "TOEM Autosplitter" can be found under the Control category. 

The Autosplitter component will detect the game with a few seconds of delay. It will automatically start your timer whenever you start a new game from the main menu
and will reset it should you go back to the main menu before finishing the game. If you have finished the game and are sent back to the main menu you will need to
reset your timer manually before the Autosplitter will work on a new run.

The Autosplitter will start the timer as soon as you get out of bed for the first time. It will split every time you enter a new region (Oaklaville, Salthamn, etc.)
and of course once you reach the "The End" screen. You should therefore set up one split for every region in the game starting with "Homelanda" and ending with
"Mountain Top".
