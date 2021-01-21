# City procedural generation

## Intro
This repository contains all the assets used to generate proceduraly a city with Unity.

## Functionnalities
The city is based on a graph (for the roads) and a heatmap for the population density at each point.

### Buildings
All the buildings are generated along the roads (edges of the graph), and they can belong to 3 diffenrent classes:
- Houses
- Building
- Skycraper
depending on the density at the location they are created.

According to its height, any building has a number of windows representing its total capacity *i.e.* the number of inhabitants that can fit in it.
For each inhabitant at time t in the building, a window is lit.

To finish for buildings, they can be either an habitation or a workplace. 

### Inhabitants
The inhabitants are NavMeshAgents, generated after the buildings and a NavMesh.

Each agent has a fixed home and workplace that are affected randomly at the population's creation. Agents only appear when they travel from home to work (or work to home), they disppear when inside of a building.

### Day and night cycle
When the game starts and after the buildings and the agents are in place, the day and night cycle starts too. It consists in a time measure increasing from 0 to 1 (reprensenting 1 day) and 2 lights for the sun and the moon. The sun and the moon rotate in order to simulate their real behaviour and their intensity vary through time.

Each inhabitant has a fixed time to start and end a day, in other words to go to work and to go back home. This important times are both randomly picked in a time range for each agent.

## Limitations
The higher the number of agents, the better limitations are visible. The main one being unexpected behaviors and delayed departures of some agents due to the numerous simaltaneous calls to `SetDestination()` on NavMeshAgents.


