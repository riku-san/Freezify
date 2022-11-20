# Freezify
Thank you for downloading the Freezify editor tool for VRChat Avatars! This was made by Riku.
If you encounter any problems, errors, or issues, please send me a message on discord: Riku-san#6036
I have a video of how to use the tool at: https://youtu.be/26DDG6dCyfU


![](https://github.com/riku-san/Freezify/blob/main/Images/Demo.gif)


# About
This is a tool for the Unity Editor that setups the ability to freeze your avatar in game natively. It sets everything up via ParentContraint and RotationConstraint components, and makes animations effect both meshes instead of just your original mesh. 
This was made in order to emulate the FreezeFrame mod from MelonLoader but natively in VRChat. However, this only works on one avatar at a time, I have successfully made a double freezable avatar and may eventually make support for it.

Warning: This tool will double your stats with an avatar and will make majority of avatars VeryPoor quality. Please be aware of this before using.
 > The Polygon count will be doubled

# Dependencies
Most recent World Constraint release - https://github.com/VRLabs/World-Constraint/releases


# Instructions
1. Duplicate your avatar by clicking on it in the hierarchy and hitting Ctrl+D
2. Remove the Animator, VRC Avatar Descriptor, and Pipeline Manager components from the second avatar
3. Rename the meshes of the avatar. This prevents animation glitches during the creation of the freezable avatar.
	> I rename my meshes by adding "Freeze" after their name. Ex: "Body" gets renamed to "BodyFreeze"
4. Place the world constraint prefab into your scene, and unpack the prefab
5. Delete the cube from the prefab
6. Drag your duplicated avatar into the container of the world constraint prefab
7. Drag the world constraint onto your avatar, at the base. It should be at the same level as the Armature and meshes
8. Drag the Reset Target from the world constraint prefab onto your avatar, same as above step
9. Now we can move onto using the tool, drag the hip bone of the original avatar into the Original Hip Bone field
10. The avatar in the world constraint is the Freeze Avatar, drag the hip bone into the field
11. Next, hit the drop down for meshes and input how many meshes your avatar has (the original avatar)
12. This is important! Make sure you drag and drop the same mesh from the original and freeze avatar into the respective fields
	> Not doing this correctly will mess up the animation set up
13. Once all meshes are assigned in the fields, Drag the root of the Freeze avatar into it's field.
	> The root is where the animator/avatar descriptor were, the very base.
14. Drag your animators from your project files into their respective fields (up to 5 max)
15. Hit Freezify and wait for it to finish

From here, the rest is setting up the avatar. The freeze animation itself will not be apart of any animator
please set up this animation in your FX animator, the animation will be under Assets/Freezify/Animations/Freeze
You will also need to set a parameter on your avatar to trigger the animation


# Known Issues
1. I've hit set up constraints/all and my rotation constraints are parented to the wrong object
  > Make sure your armatures are exactly the same between both avatars before generating.
2. My phys bones are resetting when freezing!
  > Any animated physbones will reset before the freeze animation. I'm still looking for a fix for this
  > Also note, if you have any phys bones that aren't on the bones themselves, remove those on the freeze avatar
3. My Dynamic bones aren't being deleted!
  > As of currently, there is no support for dealing with dynamic bone components. Please delete these yourself manually
4. Material change animations aren't being set up correctly!
  > Material change animations are not currently supported, you will have to recreate the animations yourself.


# What everything is
Parameters
-----------------
Original Hip Bone - The hips bone of the original avatar
Freeze Hip Bone - The hips bone of the avatar placed in the world constraint, I will refer to this as the "Freeze Avatar"
Meshes - The meshes of the avatars, used in Animation creation
Freeze Root - The root of the Freeze avatar, This is used to get the path to the freeze meshes for animating
Animators - The VRChat animator controllers, FX/Gesture/Base/etc. Drag and drop these from your Project Files
Debug Mode - Enables debug mode, provides information on operations and lets you call specific functions instead of all at onc


Buttons
-----------------
Freezify - Turns the avatar into the freezable avatar, will provide any errors if any are found
Reset - Resets the constraints on the avatar. Will not: put phys bones back, delete animations


Debug Buttons
-----------------
Setup Constraints - Sets up the ParentConstraint and RotationConstraint components on the Freeze Avatar
Setup Avatar Animations - Copies your animators and replaces the animation clips. The new animation clips
are copies of the animation clips on the animators, edited to effect both meshes
Create Freezify Animation - Creates the actual FreezeON/FreezeOFF animation for animator use
Setup All - Same as the Freezify Button above
Reset Constraints - Same as the Reset button above, just a fancier name
