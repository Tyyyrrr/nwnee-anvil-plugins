# HOW TO INTEGRATE PLUGINS INTO ANY SERVER:

1) Create 'Implementation' folder in ServerData directory, to put your concrete classes there
2) Create Bootstrapper class implementing IBootstrapper and put it in Implementation folder. It must have a single, public, parameterless constructor
3) Implement every interface defined in ServerData and its subdirectories, and organise them in Implementation directory and its subdirectories
4) Make bootstrapper methods return your concrete implementations

NOTE: Some plugins may require additional configuration via EasyConfig to load properly

# HOW TO HIDE SERVER-SPECIFIC DATA:
1) Extract your 'Implementation' folder into another private repository and remove it from the public one.
2) Create .gitmodules file in top-level directory of the public repository.
3) Add a submodule path "ServerData/Implementation" with URL pointing to the private repository

# HOW TO SYNC PRIVATE IMPLEMENTATION WITH UPDATED INTERFACE
```
git remote add sdi https://github.com/Tyyyrrr/nwnee-anvil-plugins.git
git fetch sdi serverdata-interface
git checkout sdi/serverdata-interface -- .
```