# Pilule-API

Étant donné que Pilule (wrapper humain de Capsule d'ULaval) a tiré sa révérence, j'ai décidé d'essayé de créer un petit API pour accéder à capsule.
Peut-être que ça n'aboutiera à rien, mais si j'ai du temps je vais travailler dessus.

> Prototypage : ce projet est présentement en phase de prototypage pour tester la faisabilité et les technologies (mon premier project F#), la stabilité n'est pas garantie :)

## API
Voir le fichier `api.fs` dans /api pour une vue d'ensemble des points d'entrés

### Try it
It might be down from time to time, but you can try it at
```
vaub0039.vincentaube.net:8083
```

### Authentication : Basic Authentication
Utilisez votre idul comme username, rien n'est sauvegarder côté serveur
```
GET /schedule                         # récupérer son horaire de session
GET /schedule/w2015                   # récupérer son horaire de différences session, [w/s/a]0000

GET /course/search/GLO                # recherche de cours "GLO" cette session
GET /course/search/GLO/1901           # recherche du cours "GLO-1901" cette session
GET /course/search/GLO?name=pratique  # recherche de cours "GLO" dont le nom contient "pratique"
```

## FSharp
J'ai décidé d'essayer F# comme langage pour le projet pour essayer un langage fonctionnel :)

Bien qu'utilisant .NET, je code sur OS X avec VS Code + Ionide, je ne peut donc pas dire que VS va fonctionner correctement (Xamarin Studio n'a pas de problèmes par contre).

### Mac et Linux (UNIX)
```
Mono 4.0+
build.sh [build phase if needed]
```

### Windows
```
Visual Studio 2013-2015 avec Visual F#
build.cmd [build phase if needed]
```

## WIP

- ~~Log-in fonctionnel~~
- Endpoints pour accéder à l'horaire 
- More...
