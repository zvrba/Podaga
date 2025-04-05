# Podaga
Collection of data structures and algorithms.  Introductory and API documentation is available [here](https://zvrba.github.io/Podaga/).

## About NuGet packages

The assemblies use strong-naming as a cheap (free) substitute for code signing certificates:  the strong name ensures that the
assembly has not been tampered with.  I can prove the ownership of 

```
Public key (hash algorithm: sha1):
0024000004800000140100000602000000240000525341310008000001000100e552927e424972
5fd78d638dfd21f7fc74d89b374cc1b5efd5dc3d9a0b510a75771ddb05168c53254af60ba3e3a0
dfbbd4fc2fceac6dd60ccab76a58257c32834c4556a9c8efd92c130cf403012dc7da94ee40ac0a
e4cefb1251302cff2fd4a854d30b4da6a9491d3d9356e863e1bf4dfd2b86b6d123137480c87898
4ab2df249d10d42e91d3a08f63d3bff3f32d3000ea6e1868b2d816543c4be120c729f112d3bc36
7d4fbe03ba51455db086d9970b150ab6ea07c2ea9e6376937eaceebc515675cfe502d41548f588
cb5f818f4d8bcba377505ee3ee217e50f4c0016eaeac1918cb99d472e3003542f23ab1e76f4783
16c079d114699339f22952a9a407d0

Public key token is 33475ebbe7618e8b
```

There are no separate symbols packages because the assemblies are build with `<DebugType>embedded</DebugType>`.

## About source code

All code in `main` branch is licensed under [MPL-2.0](LICENSE).

The documentation (all content in `docs` branch) is licensed under [CC BY-NC-ND 4.0](https://creativecommons.org/licenses/by-nc-nd/4.0/) license.

`Docs` is a submodule pointing to a private repository containing documentation source; you do _not_ need to check it out.

To build the code on your own:

- Remove SBDOC project from the solution. (This is the "master" documentation source maintained in private `Docs` repositoy.)
- Disable strong-naming on projects that have it turned on. (I use strong-namig as a cheap substitute for code signing certificates.)
- Optionally, disable generation of nuget packages.

