INCLUDE constants.ink
INCLUDE GameStates.ink


-> test

=== test ===

#player 
This is a dialogue test spoken by the player. 

#flower
And this dialogue is spoken by the flower.

If there are no tags, the dialogue will progress with a timer.

There is also the possibility to skip animations and the timer.

Characters can also speak without the timer.

For example... #keep_talking

...like this... #keep_talking

...line by line. #keep_talking

If we want to speak without a timer, just use the pause tag. #pause_dialogue

Using the end tag gives the same result but sends info to Unity that the script has ended. #end_dialogue

-> choices

=== choices ===

#player
I've also created a choice handler that displays dialogue options. 
You can pick them by holding Shift and selecting with the scroll wheel.

+ [Option 1]
    Picked Option 1.

+ [Option 2]
    Picked Option 2.

+ [Option 3]
    Picked Option 3.

+ [Option 4]
    Picked Option 4.

+ [Option 5]
    Picked Option 5.

- I've designed it for 2 to 5 possible choices.
The system handles it easily, regardless of the amount. 

+ Just

+ Like

+ This

- -> gamestates

= gamestates

- Unity maintains all game states and syncs them with Ink.
Because of this, dialogue options can be handled entirely within Ink... #keep_talking
...Unity doesn't need to know which option will be picked; it just receives the text and tags.

Certain objects or events will trigger specific actions. 

For example, if we pick up this item, the dialogue should play automatically.

-> try_pick_up_apple

= try_pick_up_apple
-> GameplayChoice(ApplePickedUp, -> apple_picked_up, -> try_pick_up_apple)

= apple_picked_up
Great!
You have picked up the apple.
-> ending

=== ending ===
#flower
For now, those are all the functions I've created.
New features are coming soon, but for now, the system works!

-> END