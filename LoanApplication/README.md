## Your first project with Kurrent Cloud

Welcome to Kurrent Cloud! To get you started, we've created an example application packaged as a set of Docker containers that illustrates the flow of events being appended to streams in KurrentDB.  

After you have completed the following steps, you will have: 



* Familiarized yourself with the Kurrent Cloud UI
* Connected an example application to Kurrent Cloud
* Navigated the KurrentDB Web UI and investigated events appended to streams
* Been provided with resources such as documentation, videos, e-learning, and sample code repositories, to take the next step in your Kurrent Cloud journey  

This example application:



* Uses a simple **Python application** to **connect a client** application to KurrentDB hosted in Kurrent Cloud
* Demonstrates the methods to **append (write) and read** events to/from KurrentDB
* Leverages **persistent and catch-up subscriptions** to receive streaming events
* Introduces security concepts with **SSL/TLS and IP access lists**
* Navigates the **KurrentDB Web UI to find streams and events**
* Works with **projections** to transform events from one form to another

Ready to get started?  Let’s get the demo application working in your newly provisioned Kurrent Cloud environment!


## Pre-requisites: Kurrent Cloud

You must adhere to some Kurrent Cloud requirements to access and use the sample application. Let’s step through them now:



* [Internet connectivity](#heading=h.xt8exc6orhkx) (See: Verifying Connectivity below)
* [Client Connection URL](#heading=h.ujp7k68muxnn)  (See: Accessing your Client URL below)
* Security mode where Mutual TLS (mTLS) is disabled
* The cluster you are using has System and User Projections enabled
    * If you didn't do so initially, please create a new cluster using this option.  (see image below)


![alt_text](images/image1.png "image_tooltip")


## Verifying Connectivity

You will log into the **KurrentDB Web UI** to verify connectivity. 

1. Click the Addresses panel on the **Kurrent Cloud Console UI** **Clusters** page.
2. The URL in the UI row is your unique KurrentDB Web UI address.
3. Select the link.


![alt_text](images/image2.png "image_tooltip")


After you select the link, the KurrentDB login screen should appear. If you reach this successfully, continue to step 4. If your web browser displays a **403: Forbidden** message, please skip to the “**[Adding your IP Address to the IP Access List](#heading=h.hfjipe2ehurh)**” section.  Once the actions required are complete, return to this section and repeat steps 1-3.


![alt_text](images/image3.png "image_tooltip")


4. Use your Admin Credentials to log in to the **KurrentDB Web UI**. Access them on the **Kurrent Cloud Console UI** by clicking the **Security** tab, and then choosing **Admin Credentials**. Your default username is **admin**. You can copy your default password using the **Copy** button to the right of the **Password** box.


![alt_text](images/image4.png "image_tooltip")


5. Return to the KurrentDB login screen.
6. Enter **admin** as your Username, paste your password in the Password field, and click **Sign In**. The **Dashboard** page will be displayed.


![alt_text](images/image5.png "image_tooltip")


## Adding your IP Address to the IP Access List

If you receive a **403: Forbidden** message in your web browser, please ensure your IP Address is included in the IP Access List.  To do this:



1. Navigate to the **Security** tab in your **Kurrent Cloud Console UI.**
2. Select **IP Access List** from the Security menu.
3. Then select the link for your IP Access List (in the image below, this is “Cloud Tutorial Access List.”


![alt_text](images/image6x.png "image_tooltip")


4. Verify that your IP address is listed in the **Addresses** section of your IP Access List.


![alt_text](images/image7x.png "image_tooltip")


5. Click **Edit &lt;Your Access List Name>** if your IP address is not listed.


![alt_text](images/image8x.png "image_tooltip")


6. Select **Add My IP Address** to have the console determine and add your IP address to the list, or **Add Address** to enter your IP manually. When finished, click **Update IP access list**. 


![alt_text](images/image9x.png "image_tooltip")


7. If you receive a Warning, confirm your IP access list update.


![alt_text](images/image10x.png "image_tooltip")


8. Once your IP has been successfully added to your environment, select **Details**.  The **Status** of the IP Access List should be **Active.**


![alt_text](images/image11x.png "image_tooltip")


9. Return to the [Verifying Connectivity](#heading=h.xt8exc6orhkx) section and repeat steps 1-3 to access the KurrentDB Web UI login screen. 


## Accessing your Client URL

To operate the demo application, you’ll need your **Client URL**. 



1. In the **Kurrent Cloud Console UI**, click the **Connect to &lt;ClusterName>** button.


![alt_text](images/image12x.png "image_tooltip")


2. The **Connection** pop-up will appear.


![alt_text](images/image13.png "image_tooltip")


3. Scroll down and select **Python** from the list of clients. The **Python** connection details will appear.  


![alt_text](images/image14.png "image_tooltip")


4. Click the **Copy** button to the right of the esdb+discover... Client URL. It should look similar to this:

    ```
    esdb+discover://admin:1e6628e34efe480a9e395acbafa8613c@kurrentdb.cud2g7cgdub7p6skf1dg.cucin94gdub7p6skf0i0.sites.dev.eventstore.cloud:2113
    ```


5. Save this URL for future steps. 


## Pre-requisites on your laptop



* Install Docker

Docker is a container management platform that runs on virtually any operating system. We use it to distribute a set of images that contain our sample application code. If you do not already have Docker installed, follow the instructions to install it on your machine at [https://docs.docker.com/engine/install/](https://docs.docker.com/engine/install/)



* Install Docker Compose

Docker Compose is an orchestration plug-in for Docker that allows running a set of Docker Images in a pre-defined way. If you DID NOT install Docker Desktop in the previous step, or you do not already have Docker Desktop or Docker Compose installed on your machine, follow the instructions to install it at [https://docs.docker.com/compose/install/](https://docs.docker.com/compose/install/)

To **verify that Docker and Docker Compose are successfully installed** on your computer, open a command prompt and execute the following commands. You should receive output similar to the following (note your version numbers may differ):


```
tony@TonyBook CloudTutorial % docker -v
Docker version 27.4.0, build bde2b89
tony@TonyBook CloudTutorial % docker-compose -v
Docker Compose version 2.31.0
tony@TonyBook CloudTutorial % 
```


Congratulations!  If you have reached this point, you have successfully set up Kurrent Cloud and your laptop.  You are ready to explore the sample application and start running the code in your environment.  


## Overview of Loans Application



![alt_text](images/image15.png "image_tooltip")


The sample application will implement a basic loan application processing system. The application is comprised of four components:



1. **Seeder**: The **seeder** or **Loan Request** application injects mock **LoanRequested** events using ShadowTraffic. These events provide all the details one would expect to give to a financial institution when applying for a loan, such as a mortgage, credit card, student loan, etc.
2. **Creditcheck**: The **creditcheck** or **Credit Check** application listens for **LoanRequested** events and performs a mock credit score check. For each loan request, a credit score is appended to the stream as a new **CreditChecked** event.
3. **Decider**: The **decider** or **Loan Decider** application listens for **CreditChecked** events and makes an automated decision about whether to approve or deny the loan based on the credit score (appending **LoanAutomaticallyApproved** and **LoanAutomaticallyDenied** events) or to refer the loan for human review (appending **LoanApprovalNeeded** events).
4. **Underwriter**: The **underwriter** or **Underwriting** application listens for **LoanApprovalNeeded** events and presents a **Read Model** to a human, showing the application details and allowing a manual loan approval or denial. For our example, we will automate this process, randomly choosing an **Approver Name**, and randomly choosing to append a **LoanManuallyApproved** or **LoanManuallyDenied** event.

Now that you have a general understanding of the loan application let’s get the code running on your machine!


## Step 1: Download the Docker Compose file

Download the **Docker Compose YAML** (docker-compose.yaml) file to your computer. Place this in a familiar location, such as your **Documents/CloudTutorial** folder. The file is available in our Samples repository:

[https://github.com/EventStore/samples/blob/main/LoanApplication/docker-compose.yaml](https://github.com/EventStore/samples/blob/main/LoanApplication/docker-compose.yaml)


```
tony@TonyBook CloudTutorial % ls -l
total 8
-rw-r--r--@ 1 tony  staff  1296 29 Jan 11:30 docker-compose.yaml
tony@TonyBook CloudTutorial % 
```



## Step 2: Set the environment variable

The **docker-compose.yaml** file will look for an environment variable that will provide the **Client URL** for your database. You copied and saved this information in a pre-requisite step above.  It should be similar to the following:


```
esdb+discover://admin:1e6628e34efe480a9e395acbafa8613c@kurrentdb.cud2g7cgdub7p6skf1dg.cucin94gdub7p6skf0i0.sites.dev.eventstore.cloud:2113
```


Set the DB_URL environment variable for your platform by entering the following at the command line:



* Mac or Linux: `export DB_URL=&lt;Client URL>`
* Windows: `set DB_URL=&lt;Client URL>`

Test that the variable is set correctly:

Mac or Linux: 


```
echo $DB_URL                                                                                                                                            
esdb+discover://admin:1e6628e34efe480a9e395acbafa8613c@kurrentdb.cud2g7cgdub7p6skf1dg.cucin94gdub7p6skf0i0.sites.dev.eventstore.cloud:2113
```


Windows:


```
echo %DB_URL%                                                                                                                                            
esdb+discover://admin:1e6628e34efe480a9e395acbafa8613c@kurrentdb.cud2g7cgdub7p6skf1dg.cucin94gdub7p6skf0i0.sites.dev.eventstore.cloud:2113
```



# Step 3: Execute the Docker Compose file:

Now that you have an environment variable containing the **Client URL**, you can run the Docker Compose file to start the application. On the command line, navigate to the folder where you placed the **docker-compose.yaml** file, and execute the following:


```
docker-compose up
```


At this point, Docker images will download, the services will start, and events will begin flowing through the system:


```
...
decider-1      | INFO:utils:Database TLS is enabled, fetching certificate details...done.
creditcheck-1  | INFO:utils:Database TLS is enabled, fetching certificate details...done.
seeder-1       | INFO:utils:Database TLS is enabled, fetching certificate details...done.
underwriter-1  | INFO:utils:Database TLS is enabled, fetching certificate details...done.
decider-1      | INFO:deciderCreating subscription...
creditcheck-1  | INFO:creditcheck:Waiting for LoanRequest events
decider-1      | INFO:decider:Waiting for events
seeder-1       | INFO:seeder:Seeding database...
underwriter-1  | INFO:underwriting:Waiting for events
decider-1      | INFO:decider:Received event: id=58b95e7d-d9f8-4f3b-a57f-7df49fba9caf; type=CreditChecked; stream_name=loanRequest-f2f2e9e1-1947-4c1e-afcf-c317e8cc02db; data='b'{"Score": 4, "NationalID": null, "CreditCheckedTimestamp": "2025-01-29 16:45:43.851140"}''
creditcheck-1  | INFO:creditcheck:Received event: id=18981572-ad52-48dc-b29b-b8fb9548edad; type=LoanRequested; stream_name=loanRequest-18981572-ad52-48dc-b29b-b8fb9548edad; data='b'{"LoanRequestId": "18981572-ad52-48dc-b29b-b8fb9548edad", "LoanRequestedTimestamp": 1738168933.527328, "Name": "Elizabeth Garcia", "Address": {"Street": "9831 Kennedy Locks Apt. 595", "City": "West Andreaburgh", "Region": "Maine", "Country": "Andorra", "PostalCode": "87570"}, "NationalId": "083-53-6101", "Age": 47, "Gender": "M", "MaritalStatus": "Widowed", "Dependents": 1, "Amount": 46867, "LevelOfEducation": "Doctorate", "Income": 892162, "JobTitle": "Building control surveyor", "JobSeniority": 38, "EmployerName": "Brown, Rivera and Hansen"}''
creditcheck-1  | INFO:creditcheck:Processing credit check - CreditChecked for NationalID None with a Score of 1
decider-1      | INFO:decider:Processing loan decision for Allen Hunter with Score of 4 - LoanApprovalNeeded
decider-1      |   Appending to stream: loanRequest-f2f2e9e1-1947-4c1e-afcf-c317e8cc02db
decider-1      | 
creditcheck-1  | INFO:creditcheck:Received event: id=05b6f06c-7b18-459a-87d8-c1b2dae9d69c; type=LoanRequested; stream_name=loanRequest-05b6f06c-7b18-459a-87d8-c1b2dae9d69c; data='b'{"LoanRequestId": "05b6f06c-7b18-459a-87d8-c1b2dae9d69c", "LoanRequestedTimestamp": 1738168948.685551, "Name": "Mr. David Olson", "Address": {"Street": "66869 Fox Extensions Apt. 199", "City": "Jessicamouth", "Region": "Delaware", "Country": "Martinique", "PostalCode": "74374"}, "NationalId": "866-12-9170", "Age": 21, "Gender": "M", "MaritalStatus": "In a relationship", "Dependents": 4, "Amount": 17624, "LevelOfEducation": "Bachelor\'s Degree", "Income": 779249, "JobTitle": "Barrister\'s clerk", "JobSeniority": 37, "EmployerName": "Torres-Dunn"}''
decider-1      | INFO:decider:Received event: id=b3b9bc85-cc9d-4853-b566-07913b3bf14d; type=CreditChecked; stream_name=loanRequest-18981572-ad52-48dc-b29b-b8fb9548edad; data='b'{"Score": 1, "NationalID": null, "CreditCheckedTimestamp": "2025-01-29 21:35:04.809190"}''
creditcheck-1  | INFO:creditcheck:Processing credit check - CreditChecked for NationalID None with a Score of 7
decider-1      | INFO:decider:Processing loan decision for Elizabeth Garcia with Score of 1 - LoanAutomaticallyDenied
decider-1      |   Appending to stream: loanRequest-18981572-ad52-48dc-b29b-b8fb9548edad
underwriter-1  | INFO:underwriting:Underwriting needed:
decider-1      | 
underwriter-1  | INFO:underwriting:What is the approver's name? Jane
underwriter-1  | INFO:underwriting:Would Jane like to approve this loan (Y/N)? N
underwriter-1  | INFO:underwriting:  Processing loan decision - LoanManuallyDenied
underwriter-1  | 
...
```


NOTE: The above output is an example. Your output will vary, but all four processes should be running.


## Step 4: Verify the output in the web UI

Return to the **KurrentDB Web UI** in your web browser. Navigate to the **Stream Browser** tab. Note that streams of **loanRequest-&lt;eventID>** are being created in the **Recently Created Streams** table:


![alt_text](images/image16.png "image_tooltip")


**Clicking a stream name** shows the events in that stream.


![alt_text](images/image17.png "image_tooltip")


Clicking **JSON** next to an event displays the complete event information.


![alt_text](images/image18.png "image_tooltip")



## Next Steps

Thanks for checking out our example application!  Please continue experimenting.  

To **explore the code, see the complete repository** at:  [https://github.com/EventStore/samples/tree/main/LoanApplication/Python](https://github.com/EventStore/samples/tree/main/LoanApplication/Python). 

You can download this code, modify and augment it, and use it to start building your own application.

To learn more about KurrentDB, check out the **completely free content at Kurrent Academy**:  [https://academy.kurrent.io/](https://academy.kurrent.io/). (If you are interested in a hands-on exploration of some KurrentDB features, we recommend [Developing event-driven applications with KurrentDB](https://academy.kurrent.io/course/application-development-fundamentals), but there are many other topics to explore.)

Ready to dive deep? See the **Kurrent Developer Documentation** available at [https://developers.kurrent.io/](https://developers.kurrent.io/)
