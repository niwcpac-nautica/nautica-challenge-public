# Challenge Environment Information

## Background

The NAUTICA challenge environment is what we will be using for our capstone challenge.  It's built around a first person shooter (FPS) microgame, which is released by Unity as a tutorial.  We repurposed it to use for training and running agents in.  Participants will use this environment to train agents in.  Currently we are training against enemy robots which are included in the game.  The initial challenge will be to learn to kill as many robots as possible, under increasing difficulty and a time limit.

## FPS Microgame Info

To learn more about the FPS microgame (not required for training agents) it can be accessed under the "Learn" tab of Unity Hub.  We basically took that project, added ML Agents to it, and then modified it and added our own code to it.  We also created new levels which we will use for both training and inference / evaluation.

## Environment Details

There are multiple Unity "scenes" that can be opened, which correspond to different levels, Level0, Level1, Level2, Level3.  There are two sets of levels, one for training, and one for the challenge.  Similar to a dataset with a test set, we will tweak the challenge levels so they are different from the training levels.  When scoring participants' agents, we will use the challenge levels.

The various levels are not configured for agent training yet and still a work in progress.

There is a simple scene called "training-test" which is configured for training, and can be used for training agents and to familiarize with the environment.  Expect the other levels to be ready in the coming days/weeks.

## Code Repository

All code is public releaseable, and non-sensitive.  We will post to Github.  If you run into issues, bugs, etc post an issue to the repo on github and we will try to support.

TODO:  public facing Github URL here

## Agent Details
**BotKiller Agent and AbstractNauticaAgent**

There is a sample agent called "BotKiller".  It derives from an abstract base class called "AbstractNauticaAgent.cs".  The abstract base class contains the code needed to interface with the FPS microgame, and the intent is for participants to derive from it, overriding methods with your own code.  The Botkiller agent can be looked at as an example, and will be used as a baseline agent.

**Observations**

The BotKiller agent is configured with the following observations:

VectorSensor (agent observations)

- Agent health (normalized between 0-1)
- Agent gun charge (normalized between 0-1).  Note that the game currently has infinite ammo, but automatically recharges like a battery.  When the charge is at 0, the agent can't shoot, and must wait for the gun to charge up.

BufferSensor - Enemies (agent observations of enemies, variable length using Attention, meaning each enemy will have the observations below associated with it)

- Distance to enemy (normalized between 0-1).  Note that this is currently normalized based on some constant based on level size.  This will probably change soon based on raycast length.
- Angle (relative bearing) to enemy (normalized from -1 to +1, where 0 is straight ahead, -1 is negative 180 degrees, +1 is positive 180 degrees, relative to the agent's current facing direction (i.e.  egocentric)
- Angle (heading) of the enemy (normalized fro, -1 to +1, where 0 is pointing directly at the agent, -1 is negative 180 degrees, +1 is positive 180 degrees)
- Enemy Health (normalized between 0-1)
- Enemy Line of Sight (LOS).  0 if the agent has an obstacle between itself and the enemy, 1 if there is a clear path

BufferSensor - Pickups (agent observations of pickups, variable length usingAttention, meaning each pickup will have the observations below associated with it)

- Distance to pickup (same as enemy, described above)
- Angle (relative bearing) to pickup (same as enemy, described above)

**Unity Sensor Components**

Unity has built-in sensor components that are handled "behind the scenes" – you do not have to write code for these.  HOWEVER if you are using a direct Python interface (either OpenAI Gym wrapper interface, or the Unity ML Agents Low Level Python API) these sensors show up as a separate vector.  If we support this, we will create an agent with all sensors attached, so participants can pick and choose which observation data they wish to use in their agent algorithm.

**Ray Perception Sensor**

The BotKiller-raycast agent uses a RayPerceptionSensor (raycasts) with the following configuration:

- 12 rays on the left and right, 1 ray forward.  Total = 25 raycasts
- 3 Detection tags – this means the raycasts will collide with objects in the game configured with these tags.
  - There are tags for Enemies, Pickups, and Obstacles.
  - I believe the way this is handled is a set of raycasts (25) per tag.  So total = 25 x 3 = vector of 75 values
  - Values are distances from agent outward until it hits something or reaches its max distance.
  - Current max distance is 25 (may change in the future)


**Grid Sensor**

The BotKiller agent uses a Grid Sensor.  This dynamically creates a gridworld, populated with values depending on what it is configured to detect.  The built-in trainers using ML Agents will then use a CNN on the grid, to take advantage of the rectangular data arrangement.

- 28x28x1 grid.  Total = 784 values.  May change in the future
- Each cell in the grid represents a 2 square unit portion of the map.  May change in the future.
- 3 Detection tags (same as the Ray Perception Sensor – Enemies, Pickups, Obstacles)
  - I believe this is handled as 3 separate grids, with one-hot values, where 1 means a detection occurred in that grid cell, and 0 means no detection.
  - a separate grid per tag means 784 x 3 = vector of 2352 values
  - Have not investigated this, the data may be separated into 3 vectors of 784 values each, or 1 vector of 2352 values.
- The grid auto-rotates with the player.  This means if the player turns 90 degrees, the grid is updated relative to the player facing.  This means "up" on the grid is always the forward pointing direction of the player.

**Camera Sensor, Render Texture Sensor**

We have not implemented Camera Sensors or Render Texture sensors yet.  Most likely when we do, we will use a first person camera view, since this is what a human player would see.  We may provide image segmentation with different colors representing the same detection tags as the previous sensors (Enemies, Pickups, Obstacles).

Most likely if/when implemented, we will use a typical image size of 84x84 pixels.  This will mean 84x84x3 (3 color channels R,G,B) = 21,168 values.

We will most likely include a raw camera sensor (21,168 values) and another image segmented sensor (an additional 21,168 values).

**Observations Notes**
- You can modify agents in Unity / C# to change what observations you want to use
- Override the CollectObservations() method in your agent.
- If you have requests for an observation data that you think would make sense for all participants to have, please let us know and we can look at implementing.
- If you need help with an idea you have for some observation but don't know how to add it within Unity / C#, let us know as well.

## Actions

The agent is configured for 5 discrete action "branches".  If unfamiliar, this means you can have 5 simultaneous actions which are orthogonal to each other.  For example you may have an action to move forward/backward, as well as a simultaneous action to turn left/right.  This means you can turn while moving.  The actions we currently have configured are:

- Movement (3 Discrete actions):  0 = Do Nothing, 1 = Move forward, 2 = Move Backward
- Strafe Movement (3 Discrete actions):  0 = Do Nothing, 1 = side step to Left, 2 = side step to Right
- Gun Shooting (3 Discrete actions):  0 = Do Nothing, 1 = Fire gun, 2 = reload (not used yet)
- Turn left/right (3 Discrete actions):  0 = Do Nothing, 1 = turn left, 2 = turn right
- Turn up/down (3 Discrete actions, NOT USED YET):  0 = Do Nothing, 1 = look upward, 2 = look downward

NOTE:  We ask that you do NOT modify actions.  This will make agents incompatible with each other, and would open the door to "cheating" – since Unity is very flexible, it would be easy to create an action to reset your health, or instantly kill enemies, etc.

## Rewards

The BotKiller example agent currently is set with a few rewards.  We encourage participants to do reward shaping on their own.  You can use the example agent as a starting point.  Values subject to change.

- Positive reward based on shooting enemy.  Upon hitting enemy, reward is equal to damage (percent) invoked, i.e. hitting an enemy for 50% of its health confers +0.5 reward.  Max is +1.0 per enemy
- Negative reward based on taking damage.  Upon getting hit, reward is equal to damage (percent) received, i.e. getting hit for 50% of your health confers -0.5 reward.  Max is -1.0 total.
- Additional +1 for killing all enemies in a level before time limit (triggers environment episode reset)
- Additional -1 for dying before time limit (triggers environment episode reset)
- If agent survives the time limit but does not kill all enemies or die, it receives +0.
- Agent receives -1 / MAX_STEPS reward per step.  This is the "pants on fire" time penalty commonly used to encourage agents to act.  Max is -1.0 total, upon reaching MAX_STEPS

 Rewards Notes
- If you need help implementing an idea for some type of reward but don't know how to do it within Unity / C#, let us know and we can try to help.

## Training Details
**Scene information**

You can open the repository in Unity and open different scenes.  These are located in Assets/Scenes.  Here is some information about the "training-test" scene you can use for training.

- Scene consists of a small room, with 2 enemy robots and the agent itself.
- Enemy bots start off (spawn) facing away from the agent
- Agent spawns facing toward the enemies.
- Agent is configured with a shotgun that has 2 shots before recharging.  Recharge speed is fairly quick.

**Builds**

To create a build (executable), you can open File → Build Settings (Ctrl+Shift+B) to bring up a window which lets you create a build.  Ensure that the scene you have open is in the build list.  If not, click the "Add Open Scenes" button to add it.  You only want ONE scene checkmarked in the list.  It should be the "training-test" scene for now.

**Prefabs**

There are various prefabs that come with the FPS Microgame.  We have modified some of them and placed them under "Assets/Prefabs".  We create prefab variants to modify existing FPS Microgame prefabs so we don't break anything.  The prefab variants we use also have special effects such as sound effects, particle effects, and others disabled.  This is in the interest of conserving memory/resources when training agents.

If you create your own Agent script and other components, you will need to create your own prefab.  We recommend creating a prefab variant based on the BotKiller agent prefab, and then customizing from there.  To do this, right-click the BotKiller prefab and select "Create → Prefab Variant".  Once created, you can modify this prefab as you see fit.  You would need to do this to add your own Agent code to the prefab.

**Training Manager**

The Training Manager in the training-test scene manages the spawning and reset of the environment during training.  To have it spawn your agent, you will need to have a prefab containing your agent (see above).  Drag this into the TrainingManager's "Team 1 Agent Prefabs" slot.

During testing, the Training Manager has a checkbox labeled "Human Control".  This is for our own internal testing when we play through the environments.  When training agents, this needs to be UNCHECKED (turned OFF).

**Other Information**

It's difficult to anticipate all participant information needs.  If we have missed something or you need more info, please let us know and we can add it here.

## Training Methods

We currently support training using Unity ML Agents "mlagents-learn", which uses the built-in PPO, SAC, etc trainers and has support for things like Curiosity, Visual observations, imitation learning, curriculum learning, RNNs, LSTMs, and others.  If you need help with any of these topics, we encourage you to not only let us know, but post in the Ms-Teams (Flank Speed, etc) NIWC_Pacific-NAUTICA team, where others in NAUTICA may also be able to help.  Remember we're trying to foster this community of interest!

If you are unfamiliar with Unity and ML Agents, you can refer to the NAUTICA lectures 1 thru 8 to get a good overall background.  However if you would like to focus only on Python training, we are investigating supporting this as well.  You can interface with Unity ML Agents environments through the ML Agents gym wrapper, which provides an openAI gym interface.  There is also an ML Agents Low Level Python API.  Both have been briefly touched on in the NAUTICA lectures.  However one issue with this approach is how to save a model to a format which is compatible with Unity.

Unity ML Agents uses ONNX (open neural network exchange) format.  We are currently investigating the possibility of training agents in python and then saving/exporting the model to ONNX, and then importing that into Unity.  There are technical challenges involved, such as the Unity Barracuda inference engine supports ONNX but may not support all NN model features and architectures.  We will be looking into this but please consider this as an UNSUPPORTED use case for now.  If we do support it in the future, we will have limited support only.

## Final Words

Have fun and experiment!  Questions, problems, comments, issues can be posted on Flank Speed Ms-Teams in the NIWC_Pacific-NAUTICA team.
