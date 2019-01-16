How to use a custom loader progress screen
==========================================

Current versions of the Tooll player allow to override the default progress bar
with a custom scene that's defined in its own operator. Here's a short tutorial
on how to use that:


## Define the loader screen's contents

Design the loader screen in Tooll as usual. You may use any operators you like,
but please try to use not too much of them! When using a custom loader screen,
the operators that are required to draw that screen are themselves loaded
without any progress indication, and you want the screen to load as quickly as
possible.

You can animate the loader screen: It starts at the time index -1 second
(yes, *minus* one second; that's one second before the start of the music
timeline) and ends at 0 seconds. It will not be played in one second though;
instead, the current loading progress is mapped to the time. In other words:
When the loader starts, the scene will be rendered as it is at t=-1s in the
editor; when loading is finished, it will be rendered at t=0s. You can think
of the loader as "the thing that's shown immediately before the demo".


## Turn the loader screen into an operator of its own

If not already done so, turn the loader screen (and *only* the loader screen)
into an operator ("Combine" command in the context menu of the Composition
window). Make sure it has one output of type "Scene".

The operator may or may not be part of the main demo's operator graph, i.e.
you may use it like any other scene in your composition, but you don't need
to connect its output to anything. (It's nevertheless useful to just have it
lying around somewhere in your composition though, if only to quickly access
it while working in the editor.)


## Note the loader operator's GUID

Find out the GUID of your loader operator. The easiest way to do this is select
it, copy it to the clipboard and paste the clipboard in a text editor. You will
get a bunch of JSON data; the `MetaID` line is what you're looking for.


## Configure the loader operator's GUID in the project settings file

Close the Tooll editor (so it won't overwrite the file you're going to edit)
and open `Config/ProjectSettings.json` in a text editor. Set the entry
`LoaderProgressOpeator` to the GUID you noted; if such an entry doesn't
exist, create it. The result should look like something like this:

    "LoaderProgressOperator": "1e784f0c-595d-4aa4-b4e6-ca0e558b55c5"


## Done!

Now save the file and test the demo in the player. It should now show your
custom operator instead of the normal progress bar while loading.


------------------------------------------------------


## Known Issues

* Progress isn't updated during the pre-caching phase at the end of the
  loading process (if enabled).

* The default camera settings aren't correct. If your loader scene renders a
  full scene (not just a `Layer2d`) and doesn't use a `Camera` operator by
  itself, please add one explicitly and leave it at the default settings.
