using System;
using UnityEngine;

namespace UnityModule.Settings {

    public partial class EnvironmentSetting {

        public partial class EnvironmentSetting_Path {

            /// <summary>
            /// デフォルトの git コマンドパス
            /// </summary>
            private const string DEFAULT_COMMAND_PATH_AWS = "/usr/local/bin/aws";

            /// <summary>
            /// git コマンドへのパスを保存している環境変数のキー
            /// </summary>
            private const string ENVIRONMENT_KEY_COMMAND_AWS = "COMMAND_AWS";

            /// <summary>
            /// git コマンドのパスの実体
            /// </summary>
            [SerializeField]
            private string commandAws;

            /// <summary>
            /// git コマンドのパス
            /// </summary>
            public string CommandAws {
                get {
                    if (string.IsNullOrEmpty(this.commandAws)) {
                        this.commandAws = Environment.GetEnvironmentVariable(ENVIRONMENT_KEY_COMMAND_AWS);
                    }
                    if (string.IsNullOrEmpty(this.commandAws)) {
                        this.commandAws = DEFAULT_COMMAND_PATH_AWS;
                    }
                    return this.commandAws;
                }
            }

        }

    }

}