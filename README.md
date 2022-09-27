## The Game of War

This is an api that allows consumers (client apps) to play the card game of War.
Rules are [here](https://bicyclecards.com/how-to-play/war/)

## How to run the api:
### Prerequisites:
* Docker

### Steps:
* Clone the repo from https://github.com/omexus/take-home_war
* From the root folder (take-home_war) run ```docker-compose -f war/Docker-compose.yml up -d ``` 
(this will spin up two containers: war-api & war-db)

### Api Url:
* http://localhost:5671
### Api Documentation (Swagger):
* http://localhost:5671/swagger/index.html


(You can use the api from any rest client, e.g. postman, etc or use the swagger interface above)


## How To Play
### Start a new match
1. Any player can start a 'new' match with:  
```POST``` http://localhost:5671/match/new
  * payload (optional)
```
{
  "id": "string",
  "name": "string"
  }
```
> id: player id (if one has been generated before and player wants to generate another match). Pass Null if this is the first time you are playing
> name: Player name (future implementation ;) )

### Browse  opened/ready-to-play matches
A player can join an existing match. To list all opened matches use:    
* ```GET``` http://localhost:5671/match/list

### Get info about a match
Any player can get information about an existing match with. In a client implementation this 
would be the endpoint that would need to be polled every 'n' seconds
* ```GET``` http://localhost:5671/match/{matchid}


### Join an existing match
Either player can join an existing match via  
* ```PUT``` http://localhost:5671/match/{matchid}/join/{playerid}
* payload (if player already exists):
```
{
  "id": "string",
  "name": "string"
  }
```
### Start a match
Once a match has two players, it can be started by either player with
* ```PUT``` http://localhost:5671/match/{matchid}/start/{playerId}
* (optional) payload (if player already exists):

### Play a card
Starting a match won't automatically draw a player's card, it would need to be explicitly called via
* ```PUT ``` http://localhost:5671/match/{matchid}/draw/{playerId}

### Player all-time wins
To get stats on a player, call:
* ```GET ``` http://localhost:5671/player/{playerId}


## Notes
* A player can start (open) unlimited matches - this could be a potential setting for a match
* The only way to add a player is by starting or joining a match
