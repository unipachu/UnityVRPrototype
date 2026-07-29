# VRPrototype

## Overview

**VRPrototype** is a Unity project used to experiment with and evaluate different approaches for physics-based interactions in virtual reality (for fun and for later use in other projects).

## Requirements

* **Unity Version:** `6000.3.20f1`
* **Hardware:** A VR headset and compatible VR controllers are required.

## Project Structure

Because this project is intended for experimentation, it intentionally contains multiple physics grab systems with overlapping functionality. This is so that different approaches could be compared with each other. These physics grab systems are categorized in high level categories:

* **ECS:** DOTS ECS-based grab system where grabbables are moved using custom spring forces (WIP).
* **Grabbable Joint Driven (GrblJntDriven):** GameObject-based system where grabbables are driven directly by ConfigurableJoints connected to the world.
* **Hand Joint Driven (HandJntDriven):** GameObject-based system where grabbables are attached to physics hands via ConfigurableJoints. The physics hands are then driven by separate ConfigurableJoints connected to the world. This grab system can be less stable than the Grabbable Joint Driven system because it uses more physics constraints.

To distinguish between similar systems, many identifiers became quite long. To keep names manageable, a number of abbreviations are used throughout the project. The table below lists the most common abbreviations.

## Common Abbreviations

|Abbreviation|Meaning|
|-|-|
|`dbl`|double|
|`grb` / `grbs`|grab / grabs|
|`grbl` / `grbls`|grabbable / grabbables|
|`grbr`|grabber|
|`jnt`|joint|
|`lcl`|local|
|`phys`|physics|
|`piv`|pivot|
|`pt`|point|
|`sgl`|single|
|`spc`|space|
|`st`|state|
|`tgt`|target|
|`trf`|transform|
|`vis`|visual|
|`wld`|world|
