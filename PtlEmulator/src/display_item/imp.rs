use std::cell::RefCell;

use glib::Binding;
use gtk::subclass::prelude::*;
use gtk::{glib, CheckButton, CompositeTemplate, Label};

// ANCHOR: struct_and_subclass
// Object holding the state
#[derive(Default, CompositeTemplate)]
#[template(resource = "/display_item.ui")]
pub struct DisplayItem {
    #[template_child]
    pub completed_button: TemplateChild<CheckButton>,
    #[template_child]
    pub content_label: TemplateChild<Label>,
    // Vector holding the bindings to properties of `ClientObject`
    pub bindings: RefCell<Vec<Binding>>,
}

// The central trait for subclassing a GObject
#[glib::object_subclass]
impl ObjectSubclass for DisplayItem {
    // `NAME` needs to match `class` attribute of template
    const NAME: &'static str = "DisplayItem";
    type Type = super::DisplayItem;
    type ParentType = gtk::Box;

    fn class_init(klass: &mut Self::Class) {
        klass.bind_template();
    }

    fn instance_init(obj: &glib::subclass::InitializingObject<Self>) {
        obj.init_template();
    }
}
// ANCHOR_END: struct_and_subclass

// Trait shared by all GObjects
impl ObjectImpl for DisplayItem {}

// Trait shared by all widgets
impl WidgetImpl for DisplayItem {}

// Trait shared by all boxes
impl BoxImpl for DisplayItem {}