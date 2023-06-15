# Milkshake-Simulator
A Discord Bot for meme generation, based specifically on the namesake [Milkshake Simulator](https://github.com/MateusF03/MilkshakeSimulator).

## Requirements
Milkshake Simulator requires .NET 7 and [Milkshake.NET](https://github.com/nyetii/Milkshake.NET) for functioning.

## Setting up
1. Firstly, you need some database. By default, the bot uses **SQL Server**.
2. You'll also need to create a bot user on Discord and get its Token, in case you don't know how, [follow this guide](https://www.writebots.com/discord-bot-token/).
3. Having the Token and Database string in your hands, you just need to run the **Milkshake.exe** file and type in them on set up.
4. If everything has gone well, the bot should run and print the list of commands on console.
5. Invite the bot to your server and type the "m!refresh" command.
6. At this point, you only need to type "/meta server" and "/milkshake", then the bot is good to go with every command available to the server.

## Documentation
Proper documentation and guide will be written after I'm done with this semester on college.
The core of the library revolves around the *Milkshakes*, which are the main objects, they are:
1. **The Source** - Any image added with the intent of serving as the "protagonist" of a generated image. A source can have tags to specify its type.
2. **The Template** - The image used as a base for the Generation. Sources will be placed either over or under the Template.
3. **The Topping** - A set of properties inherently dependant of the designated Template. A topping can also have tags.
4. **The Generation** - It is the resulting *Milkshake*, being the metadata of the generated image.

### Tags
There are 8 tags the user can choose:
1. Any
2. Person
3. Symbol
4. Object
5. Shitpost
6. Picture
7. Post
8. Text

## Credits
This project is totally inspired on my friend Mateusão's [Milkshake Simulator](https://github.com/MateusF03/MilkshakeSimulator) project, which is also a Discord bot with the same idea.