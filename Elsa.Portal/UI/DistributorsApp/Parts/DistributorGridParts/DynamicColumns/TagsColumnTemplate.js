

this.removeTag = (assignment) => {

if (assignment.unassign) {
    assignment.unassign();
    return;
}

if (!confirm("Opravdu chcete odebrat štítek \"" + assignment.TagTypeName + "\"?"))
    return;

lt.api("/CustomerTagAssignment/Unassign")
    .query({"customerId": assignment.CustomerId, "tagTypeId": assignment.TagTypeId })
    .post(() => {
        if (assignment.afterUnassigned)
            assignment.onTagAssignmentChanged();
        });
    };

this.openTransitionPopup = (assignment, targetPicker) => {
    if (!assignment.HasTransitions)
        return;

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
        });

        Popup.open(targetPicker);
    });
        };

this.transitionSelected = (transition) => {

    if (transition.doTransition) {
        transition.doTransition(transition);
        Popup.close(targetPicker);
        return;
    }

    lt.api("/CustomerTagAssignment/assign")
        .query({ "customerId": transition.CustomerId, "tagTypeId": transition.TagTypeId })
        .post(() => {
            transition.onTagAssignmentChanged();
            Popup.close(targetPicker);
        });

};
  
