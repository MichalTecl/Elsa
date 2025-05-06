app = app || {};
app.CrmRobots = (self) => {

    self.robots = [];
    self.lastEditedRobot = null;
    self.robotsPopupOpen = false;
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

    self.openRobotsPopup = () => {
        self.robotsPopupOpen = true;
    }

    self.closeRobotsPopup = () => {
        self.robotsPopupOpen = false;

        self.filterRobots(null);
    }

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

    self.editRobot = (robotVm) => {
        self.lastEditedRobot = robotVm;
        app.Distributors.app.importSavedFilter(robotVm.Filter);

        self.closeRobotsPopup();
    };

    self.openRobotSetup = () => {        
        self.lastEditedRobot = self.lastEditedRobot || {};
        self.robotSetupPopupOpen = true;
    };

    self.cancelRobotSetup = () => {
        self.robotSetupPopupOpen = false;
    };

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