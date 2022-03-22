# WiinUPro

WiinUPro was started by KeyPuncher [here](https://github.com/KeyPuncher/WiinUPro).

## Goals
This fork aims to replace the older SCP driver used in the original project with the newer ViGEm bus. The SCP driver can cause complications with other inputs while the newer ViGEm bus avoids these issues and is used by many current controller emulation projects.

If replacing the SCP driver with ViGEm is a success I will also aim to replace vJoy with ViGem to remove the need to install vJoy as well.

## Status
SCP driver usage has been replaced with ViGEm in WiinUPro, but WiinUSoft has not yet been updated.

During updates to WiinUPro I found that WiinUPro still requires a significant amount of work to fix some critical bugs. Most especially a null reference that occurs when connecting a Wii U Pro controller in Release mode (doesn't occur in Debug mode).
Due to these issues and the apparent hassle of using and developing support for the Wii/U controllers, I'm putting further updates on the back burner for now.
