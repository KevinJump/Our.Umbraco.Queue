 _______  __   __  _______  _______  __   __  _______  __   __  _______ 
|       ||  | |  ||       ||       ||  | |  ||       ||  | |  ||       |
|_     _||  |_|  ||    ___||   _   ||  | |  ||    ___||  | |  ||    ___|
  |   |  |       ||   |___ |  | |  ||  |_|  ||   |___ |  |_|  ||   |___ 
  |   |  |       ||    ___||  |_|  ||       ||    ___||       ||    ___|
  |   |  |   _   ||   |___ |      | |       ||   |___ |       ||   |___ 
  |___|  |__| |__||_______||____||_||_______||_______||_______||_______|
-------------------------------------------------------------------------
						For Umbraco v8


	Thanks for installing TheQueue for Umbraco 8. You are now just a
	few simple steps away from sending things to the background queue.


	1. Give users/groups send to queue permissions. 

	   if you are admin then you should get SendToQueue permissions
	   by default. but other user groups will need this permission 
	   adding in the User section of umbraco.

	2. Send to Queue from the menu.

	   for content you should see a Send to Queue option in the 
	   context (right click) menu on content, this is where you 
	   send things to the queue

	3. View and process the queue

	   You can view the queue from teh dashboard in the Settings
	   section. you can also process the queue from here.

	   by default the queue will process in the background every
	   60 seconds. 


	bonus: conifg settings (web.config > appSettings )

	- Turn of the background processing of the queue: 
	    <add key="TheQueue.BackgroundProcessing" value="true" />
    
	- Change how often the queue polls 
		<add key="TheQueue.BackgroundPeriod" value="120" />

	- Change how many items get done in one 'request'. If publishing
	  is too slow you may get timeouts processing to many files:
		<add key="TheQueue.BatchSize" value="200"/>
