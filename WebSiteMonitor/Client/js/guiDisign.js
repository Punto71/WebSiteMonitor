var editForm = {
    view: "form",
    id: "editForm",
    borderless: true,
    elements: [],
    rules: {},
    elementsConfig: {
        labelPosition: "top",
    }
};

var formButtons = {
    cols: [{ view: "button", type: "form", value: "Save", hotkey: "enter", id: "saveButton", height: 35 }, { view: "button", hotkey: "escape", value: "Cancel", id: "cancelButton", height: 35 }]
}

var modalEditor = {
    id: "modalEditor",
    view: "window",
    width: 300,
    headHeight: 52,
    position: "center",
    modal: true,
    body: webix.copy(editForm)
};

var searchCtrl = { view: "search", id: 'search', labelWidth: 100 };
var addWebSiteBtn = { view: "button", id: 'addWebSite', labelWidth: 110, value: "Add new", tooltup: "Add new website", width: 120 };
var logOutBtn = { view: "button", id: 'logOut', labelWidth: 110, value: "Logout", tooltup: "Logout", width: 120 };
var websiteTableCtrl = {
    id: "websiteTable", view: "datatable", select: false, css: "custom_grid_style", columns: [
            { id: "name", sort: "string", fillspace:true, header: ["Website", { content: "textFilter" }], template: "#name# (#url#)" },
            { id: "state", sort: "string", width: 150, header: ["State", { content: "selectFilter" }], template: stateTemplate },
            { id: "last_date", width: 140, sort: "date", format: webix.Date.dateToStr("%H:%i:%s %d.%m.%y"), header: ["Last request", { content: "dateFilter" }] },
            { id: "avg_response_time", width: 140, sort: "int", header: ["Avg response time", { content: "numberFilter" }], template: "#avg_response_time# ms" },
            { id: "availability_pct", width: 100, sort: "int", header: ["Availability", { content: "numberFilter" }], template: "#availability_pct#%" }
    ], editable: false, checkboxRefresh: true, scroll: "y", select: "row", resizeColumn: true, multiselect: false, blockselect: false
}

function stateTemplate(obj, common, value) {
    if (webix.env.touch)
        return "<img src='js/skins/image/"+ value + ".png'/>";
    else
        return "<table class='webix_cell'><tr><td><img src='js/skins/image/" + value + ".png'/></td><td>" + value + "</td></tr></table>";
};

var websiteDetailsTableCtrl = {
    id: "websiteDetailsTable", view: "datatable", select: false, css: "custom_grid_style", columns: [
            { id: "Key", sort: "date", fillspace: 1, header: ["Response date", { content: "dateFilter" }], format: webix.Date.dateToStr("%H:%i:%s %d.%m.%y") },
            { id: "Value", sort: "int", width: 150, header: ["Response", { content: "numberFilter" }], template: "#Value# ms", cssFormat: pingStyle },
    ], editable: false, checkboxRefresh: true, scroll: "y", select: "row", resizeColumn: true, multiselect: false, blockselect: false
}

function pingStyle(value, config) {
    if (config.Value > 0)
        return { "color": "rgb(120, 185, 0)" };
    else
        return { "color": "rgb(223, 75, 49)" };
};

var desktopLeft = {
    id: "first", gravity: 1.6,
    rows: [websiteTableCtrl, { cols: [logOutBtn, addWebSiteBtn, {}] }]
};

var desktopRight = {
    id: "second", gravity: 0.5, rows: [
        websiteDetailsTableCtrl
    ]
};

var mobileLeft = {
    id: "first",
    rows: [websiteTableCtrl, { cols: [logOutBtn, addWebSiteBtn, {}] }]
};

var mobileRight = {
    id: "second", rows: [
        websiteDetailsTableCtrl
    ]
};

var mobileUI = {
    rows: [{ view: "multiview", id: "mainView", cells: [mobileLeft, mobileRight] }, {
        view: "tabbar", id: "tabs", type: "bottom", multiview: true, tabMinWidth: 80,
        options: [{ id: "first", value: "List" }, { id: "second", value: "Details" }]
    }]
};

var desktopUI = { cols: [desktopLeft , { view: "resizer", css: "resizerStyle", width: 6 }, desktopRight ] };

var context = {
    view: "contextmenu",
    id: "cmenu",
    width: 215,
    data: [{ id: "edit", icon: "edit", value: "Edit" }, { id: "delete", icon: "trash", value: "Delete" },
    { id: "clear", icon: "eraser", value: "Clear history" }]
};

function attachEvents() {
    $$("addWebSite").attachEvent("onItemClick", function (id, e, node) {
        showEditor("web_site", "Add new website");
    });
    $$("logOut").attachEvent("onItemClick", function (id, e, node) {
        logout();
    });
    $$("websiteTable").attachEvent("onItemDblClick", function (id, e, node) {
        showEditor("web_site", "Edit website settings", id.row);
    });
    
    $$("websiteTable").attachEvent("onSelectChange", function () {
        if (this.getSelectedId() != undefined)
            loadDetails(this.getSelectedId().row);
    });
    $$("cmenu").attachEvent("onItemClick", function (id, e, node) {
        var c = this.getContext();
        switch (id) {
            case "edit":
                showEditor("web_site", "Edit website settings", c.id.row);
                break;
            case "delete":
                deleteElement("web_site", c.id.row, c.obj.getItem(c.id).name);
                break;
            case "clear":
                clearHistory(c.id.row, c.obj.getItem(c.id).name);
                break;
        }
    });
}