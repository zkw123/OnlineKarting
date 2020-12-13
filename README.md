# OnlineKarting
在卡丁车demo上基于Unet实现了双人局域网联机对战功能，生成的可执行文件位于Karting\GameOnline目录下，操作方式wasd控制移动，空格跳跃，shift漂移，先到达终点的玩家胜利。
使用流程：
1.在两个连接到同一局域网的终端上打开可执行文件，并关闭防火墙（需要互相能够ping通）。
2.选取一个为终端作为服务器，在服务器上点击右上角Initialize Broadcast按钮，并选择Start Broadcasting开始在局域网中广播。
3.在另一个终端（客户端）点击Initialize Broadcast按钮，选择Listen for Broadcasting开始监听，直到下方LAN Client地址与服务器一致。
4.在服务器端点击右上角LAN Host按钮建立服务器，此时画面出现变化，player1的车辆出现。
5.在客户端点击右上角LAN Client按钮进行连接，此时画面再次变化，player2的车辆出现，游戏开始。
采用了帧同步的方式和P2P的网络结构，服务器仅在关键数据例如游戏开始时间和输赢、汽车位置上与客户端保持一致，其余逻辑由客户端自行运算解决。
