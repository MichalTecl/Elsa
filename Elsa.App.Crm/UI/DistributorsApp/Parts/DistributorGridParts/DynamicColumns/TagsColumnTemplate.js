((host) => {
const toDateInputValue = (assignedText) => {
    const parts = (assignedText || "").split(".");
    if (parts.length !== 3)
        return "";

    return parts[2] + "-" + parts[1].padStart(2, "0") + "-" + parts[0].padStart(2, "0");
};

const notifyAssignmentChanged = (assignment, assignments) => {
    if (assignment.onTagAssignmentChanged)
        assignment.onTagAssignmentChanged(assignments);
};

host.removeTag = (assignment) => {

    if (assignment.unassign) {
        assignment.unassign();
        return;
    }

    if (!confirm("Opravdu chcete odebrat štítek \"" + assignment.TagTypeName + "\"?"))
        return;

    lt.api("/CustomerTagAssignment/Unassign")
        .query({ "customerId": assignment.CustomerId, "tagTypeId": assignment.TagTypeId })
        .post((assignments) => {
            notifyAssignmentChanged(assignment, assignments);
        });
};

host.openTransitionPopup = (assignment, targetPicker) => {
    assignment.noteEditOpen = false;
    assignment.noteDraft = assignment.Note || "";
    assignment.AssignDtInput = assignment.AssignDtInput || toDateInputValue(assignment.Assigned);

    if (!targetPicker.initialized) {
        targetPicker.initialized = true;
        targetPicker.innerHTML = "<div class=\"popupBody\" lazy-fill-by=\"/UI/DistributorsApp/Parts/TagTransitionTargetSelect.html\"></div>";
    }

    if (!!assignment.Transitions) {
        Popup.open(targetPicker);
        return;
    }

    lt.api("/CustomerTagAssignment/GetTransitions")
        .query({ "customerId": assignment.CustomerId, "tagTypeId": assignment.TagTypeId })
        .post((transitions) => {
            assignment.Transitions = transitions;

            assignment.Transitions.forEach(t => {
                t.doTransition = assignment.doTransition;
                t.onTagAssignmentChanged = assignment.onTagAssignmentChanged;
                t.noteInputOpen = false;
                t.note = null;
                t.isHidden = false;
                t.updateNote = (v) => t.note = v;
            });

            Popup.open(targetPicker);
        });
};

const saveTransition = (transition) => {
    if (transition.doTransition) {
        transition.doTransition(transition);
        Popup.close();
        return;
    }

    lt.api("/CustomerTagAssignment/assign")
        .query({ "customerId": transition.CustomerId, "tagTypeId": transition.TagTypeId, "note": transition.note })
        .post((assignments) => {
            notifyAssignmentChanged(transition, assignments);
            Popup.close();
        });
};

host.transitionSelected = (transition) => {

    if (transition.RequiresNote) {
        transition.noteInputOpen = true;
        return;
    }

    saveTransition(transition);
};

host.cancelNoteInput = (transition) => {
    transition.noteInputOpen = false;
};

host.confirmNote = (transition) => {
    saveTransition(transition);
    transition.noteInputOpen = false;
};

host.editNote = (assignment) => {
    assignment.noteDraft = assignment.Note || "";
    assignment.noteEditOpen = true;
    lt.notify();
};

host.updateAssignmentNoteDraft = (assignment, value) => {
    assignment.noteDraft = value;
};

host.cancelAssignmentNoteEdit = (assignment) => {
    assignment.noteEditOpen = false;
    assignment.noteDraft = assignment.Note || "";
    lt.notify();
};

host.saveAssignmentNote = (assignment) => {
    const nText = assignment.noteDraft;
    if (!nText && assignment.RequiresNote)
        return;

    lt.api("/CustomerTagAssignment/ChangeAssignmentNote")
        .query({ "customerId": assignment.CustomerId, "tagTypeId": assignment.TagTypeId, "note": nText })
        .post(() => {
            assignment.Note = nText;
            assignment.noteEditOpen = false;
            lt.notify();
        });
};

host.changeAssignmentDate = (assignment, assignDate) => {
    if (!assignDate)
        return;

    lt.api("/CustomerTagAssignment/ChangeAssignmentDate")
        .query({ "customerId": assignment.CustomerId, "tagTypeId": assignment.TagTypeId, "assignDate": assignDate })
        .post((assignments) => {
            assignment.AssignDtInput = assignDate;
            notifyAssignmentChanged(assignment, assignments);
        });
};
})(this);
