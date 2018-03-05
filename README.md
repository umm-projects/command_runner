# Command Runner

* cli コマンドを実行

## Requirement

* Unity 2017

## Install

```shell
yarn add github:umm-projects/command_runner
```

## Usage

```csharp
using UnityModule.Command;
using UnityEngine;

public class Sample {

    public void Run() {
        Debug.Log(Runner<string>.Run("git", "status"));
    }

}
```

* `Runner` クラスの型引数に `UniRx.IObservable<T>` を指定すると [UniRx](https://github.com/umm-projects/unirx) による非同期処理を行います

## License

Copyright (c) 2018 Tetsuya Mori

Released under the MIT license, see [LICENSE.txt](LICENSE.txt)

