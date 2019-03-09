  
########################################################################  
 This project is subject to the terms of the Mozilla Public  
 License, v. 2.0. If a copy of the MPL was not distributed with this  
 file, You can obtain one at http://mozilla.org/MPL/2.0/  
 Any copyright is dedicated to the NominalNimbus.  
 https://github.com/NominalNimbus  
########################################################################
  
**_NominalNimbus_ project is a client <=> server Trading Platform with stron focus on Algorithmic Trading with build in features like:**  
**+ .net Scripting Library on client side**
**+ Server Side headless deployment of trading algos**
**+ Backtesting Engine**  
**+ Marging Broker and Non-Marging Broker Simulation**  
**+ Combine real market datafeed with Broker-Account Simulation to test your trading ideas**  
**+ LMAX API for Demo & Live Trading**  
**+ Poloniex API for Live Trading**   
  
  
# ClientNimbus
  
Client application connects to ServiceNimbus to receive marketdata and order states.  
Also it manages user generated scripts which are hosted and run on Scripting Service.  
ClientNimbus contains a backtest environment to create test rules.   
=> The backtest itself runs on ServiceNimbus to keep the client resource friendly.  
  
How to run ClientNimbus:  
+ download and install Visual Studio  
+ download the sources from github  
+ build the solution  
+ run ClientApp.exe from target directory  

ClientNimbus want's to connenct to ServiceNimbus after startup.  
Enter the user and password as well as IP and designtime port: 8732 for WCF connection.  

After ClientNimbus is connected to ServiceNimbus, it forces you to create a Portfolio.  
A Portfolio is a selection of Strategies.  
A Strategy is a selection of Signals.  
A Signal is an user generated script of trading rules.  
.  
.  
.  
![clientnimbus](https://user-images.githubusercontent.com/44921994/53283183-bffe6400-3742-11e9-92d0-d4f186f4829e.png)  
.  
.  
### Donation to keep this up and running:
IOTA: FKD9BYAHVBMDDW9DQBBTOFJEHFWZNTYB9UBHPBMABACHFMGGQIBVBLLDLEWYXOGAGGVZVCPVVXHUFTJU9YGNADFNGW
ETH:  0x88920B317625fDfe27A8a2353A1173D3097083D2
