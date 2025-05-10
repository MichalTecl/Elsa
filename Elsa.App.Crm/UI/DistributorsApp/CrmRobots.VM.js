app = app || {};
app.CrmRobots = (self) => {

    self.robots = [];
    self.isEditingRobot = false;
    self.lastEditedRobot = null;
    self.robotsFilter = null;
    self.robotSetupPopupOpen = false;
        
    const receiveRobots = (robots) => {
        self.robots = robots;        

        const matcher = new TextMatcher(self.robotsFilter);
        self.robots.forEach(t => t.isHidden = !matcher.match(t.Name, true));       
    };

    const loadRobots = () => {
        lt.api("/crmrobots/getRobots").get(receiveRobots);
    };

    self.editRobot = (robot) => {
        self.lastEditedRobot = robot;
        self.isEditingRobot = true;
    };    

    self.createNewRobot = () => {

        self.lastEditedRobot = {
            "Name": "",
            "Description":""
        }

        self.isEditingRobot = true;
    };

    self.cancelRobotEdit = () => {

        const reload = !!self.lastEditedRobot.Id;

        self.lastEditedRobot = null;
        self.isEditingRobot = false;

        if (reload)
            loadRobots();
    };

    self.saveRobot = () => {

        const robot = self.lastEditedRobot;
        robot.Filter = app.Distributors.vm.filter;

        lt.api("/CrmRobots/saveRobot")
            .body(robot)
            .post(robots => {
                receiveRobots(robots);
                self.cancelRobotEdit();
            });

    }; 

    self.changeRobotActivity = (robotId, activate) => lt
        .api("/CrmRobots/ChangeRobotActive")
        .query({ robotId, activate })
        .post(receiveRobots);

    self.filterRobots = (query) => {

        if (query === self.robotsFilter)
            return;

        self.robotsFilter = query;
        receiveRobots(self.robots);
    };

    self.moveRobotSequence = (robotId, direction) => {
        lt.api("/crmrobots/moveRobotSequence")
            .query({ robotId, direction })
            .post(receiveRobots);
    }
        
    loadRobots();
};

const setupRobotsPlugin = () => {

    if ((!app.Distributors) || (!app.Distributors.vm)) {
        setTimeout(setupRobotsPlugin, 50);
        return;
    }

    app.CrmRobots(app.Distributors.vm);
}
setupRobotsPlugin();