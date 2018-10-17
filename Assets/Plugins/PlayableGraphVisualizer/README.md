# PlayableGraph Visualizer

## Introduction

The PlayableGraph Visualizer is a tool that displays the PlayableGraphs in the scene.
It can be used in both Play and Edit mode and will always reflect the current state of the graph.
Playable nodes are represented by colored nodes, varying according to their type. Connections color intensity indicates its weight.

## Setup

🚨 Be careful to use a release that is compatible with your Unity version (see table below). The `master` branch is compatible with 2018.1 and later.

There are two ways to install the PlayableGraph Visualizer:

1. Standalone:
    - Download the release compatibles with your Unity version;
    - Uncompress the downloaded file in your `Assets` direcroty.

2. Git (command line):
    - Change directory to your project's `Assets` directory.
    - Run `git clone https://github.com/Unity-Technologies/graph-visualizer`

## Usage

- Open the PlayableGraph Visualizer in **Window > PlayableGraph Visualizer**.
- Open any scene that contains at least one `PlayableGraph`.
- Select the `PlayableGraph` to display in the window's top-left list.
- Click on the nodes to display more information about the associated Playable Handle.

_Note:_
- You can show just your `PlayableGraph` using `GraphVisualizerClient.Show(PlayableGraph)` in the code.
- If your `PlayableGraph` is only available in Play mode, you will not be able to see it in Edit mode.

## Unity compatibility

 Unity version | Release
---------------|--------------
 2018.1+       | v2.2 (master)
 2017.1+       | v1.1
