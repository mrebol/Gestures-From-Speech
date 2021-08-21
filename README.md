# Gesture Generation and Animation From Speech

This code implements the gesture animation on a virtual character introduced in the publication [Passing a Non-verbal Turing Test: Evaluating Gesture Animations Generated from Speech](https://arxiv.org/abs/2107.00712):

    @InProceedings{Rebol_2021_IEEE_VR,
      author = {Rebol, Manuel and Gütl, Christian and Pietroszek, Krzysztof},
      title = {Passing a Non-verbal Turing Test: Evaluating Gesture Animations Generated from Speech}, 
      booktitle = {2021 IEEE Virtual Reality and 3D User Interfaces (VR)}, 
      year = {2021},
      pages = {573-581},
      doi = {10.1109/VR50410.2021.00082}
    } 
    
![A: Generated from our model vs B: Gestures extracted from video](https://github.com/mrebol/Gesture-Generation-From-Speech/blob/main/media/ours-vs-video.gif)

*A: Generated from our model vs B: Gestures extracted from video*

## Dependencies
+ Unity 2019.4
+ UMA 2 - Unity Multipurpose Avatar (Free Download in Unity Asset Store)


## Run

Open the project in Unity and press Play in the Unity Editor to run a test sequence.

## Configuration

Choose between different sequence from Oliver and Gruber. 

Select the Game Object `GestureManager` in the scene hierachy panel:

![Scene Hierachy Panel](https://github.com/mrebol/Gestures-From-Speech/blob/main/media/gesture-manager.png)

Choose a sequence from the dropdown menu `Selected Interval`:

![Selected Interval](https://github.com/mrebol/Gestures-From-Speech/blob/main/media/speaker-select.png)



