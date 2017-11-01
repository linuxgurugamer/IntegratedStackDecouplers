Integrated Stack Decouplers & Radial Crossfeed Control

It's always bothered me that you needed to add a separate part to do your staging.  It kind of makes sense if you are doing radial staging, but not for stacked.

So, I've written this mod to fix this shortcoming.

Essentially, it adds the ability to turn on an  integrated decoupler on all tanks which have a "top" node, with some exceptions which follow:

The mod will NOT modify a tank if:

	* A tank already has a built-in decoupler
	* A tank has a built-in fairing
	* A tank has a built-in engine

A special case is when a tank is attached to an engine.  In this case, the following rules apply:

	* If the TOP of the tank is attached to the BOTTOM of the engine, the integrated decoupler will initially be active
	* If the TOP of the tank is attached to the TOP of the engine (ie:  flip the tank upside down and attach it to the top of the engine), the integrated decoupler will NOT be available.


The integrated decouplers are installed in the TOP of a tank.

To use, build your vessel as normal, but leave off the stack decouplers.  Then, right-click on the tank where you want a decoupler, and click the button which says "No decoupler".  The button will change to say "Integrated Decoupler", along with the other options for a decoupler (Force Percent and Disable Staging).  The following images show a simple two stage rocket with the integrated decoupler being configured for each stage.

The Radial Crossfeed Control is designed to make the crossfeed in the radial decouplers more controllable.  There is one setting with three possible values:
	Crossfeed disabled	Need no explanation
	Bi-directional		Same as the stock Crossfeed enabled
	Uni-directional		Disables the crossfeed when any engines in the attached parts run out of fuel in the attached parts.

The way this works is by first getting a list of all resources used by engines (parts which have ModuleEngine or ModuleEngineFX) from the attached set of parts attached to the decoupler.  Then, during flight, those resources are monitored, if/when the any of the resources falls to zero, the crossfeed is disabled.




This mod requires Module Manager

Download: https://github.com/linuxgurugamer/IntegratedStackDecouplers/releases
Source: https://github.com/linuxgurugamer/IntegratedStackDecouplers
License: GPLv3
 

