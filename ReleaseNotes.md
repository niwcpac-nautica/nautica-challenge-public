## Final challenge 
- 26 Aug 2021 Initial release
  - Automatic agent advancement implemented for agent training and for the challenge.
  - Curriculum learning instructions available with example YAML file.
  - Fixed unassigned references exception bug.
## Beta
- 12 Aug 2021 Patch release
  - Restructured/refactored/cleaned-up Botkiller agent script for ease of code review, training, etc.
  - Known issue: Rare event where the agent may log excessive damage after a single hit, after running up to a bot and then running away.
- 29 Jul 2021 New feature release
  - All levels are ready for agent training.
    - Switching levels for agent training now easier... just update "next level" (don't change "current level").  
    - The level prefabs just need to be placed into the training scene.
  - Agent training with Google Colab Python Notebook available!  Allows users to train with Google's resources over the cloud instead of on their own computer.
    - Link to [Google Colab](https://colab.research.google.com/drive/1uWfjZ1fr1hYwsNbY-nCCnjcpwNG0aKOH?usp=sharing)
    - Make a copy of the Python notebook in your own google account.
    - Include your game build in the specified path on your google account.
## Alpha
- 23 Jun 2021
  - Our first release has brand new levels to train your agents
  - Implemented Level Design: Our first release has brand new levels to train your agents
    - They come in varying difficulty. From Level 0 to Level 4 
    - You can train your agents in an unlimited amount of time using the training levels
    - Or you can complete the Levels in a race against time to test your agent.
  - For more information regarding our alpha challenge environment, please see: [Challenge Environment Information](ChallengeEnvironmentInformation.md)
