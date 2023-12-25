The task was not perfect, as I could not devote much time to it, but it should be enough to show the basic skills. Total time spent is about 5 hours.

Basic features:

- Since the task said to store the list of files in memory on the client, there was no need to use a database, I used Redis to temporarily store the data.
- Usually I do not use Redis to organize queues, and choose either Kafka or RabbitMQ, but decided not to overload external dependencies and message queue also placed in Redis
- To notify the user that the file is ready I used signalR, maybe it's a bit overkill for a simple project, but doing updates through regular polling is not a good solution.
- As a file storage I made a simple project "DumbFileStorage" - it just writes files to disk. In a real case it should be replaced by a normal file storage, for which an adapter implementing IFileStorage should be made. You should add DumbFileStorage__StoragePath parameter to define the path to the folder where the files will be stored
- To avoid long browser download in production I added `BrowserPath` parameter, it is required to pre-install the browser in the release image and define the path to it. Otherwise the first request will be long
- I made two possibilities for scaling:
    - I split the project into Api and Processor. Api accepts requests and gives data, Processor does file conversion. You can deploy multiple instances of Processor to speed up file processing
    - If necessary, you can deploy several Api instances as well, in this case you need to specify a unique parameter `CacheNames__DoneSet` to each of them so that the result would come to the one from which the task was sent
- The task didn't say anything about unit tests. I wrote a few things to show that I know how to write them, but much less than required.
- I have quite a lot of experience in HTML layout, but I haven't done it for several years. If necessary, the skill will quickly recover, in the task did not try hard with it, took bootstrap that would look normal