mod imp;

use glib::{BindingFlags, Object};
use gtk::prelude::*;
use gtk::subclass::prelude::*;
use gtk::{glib, pango};
use pango::{AttrInt, AttrList};

use crate::client_object::ClientObject;

// ANCHOR: glib_wrapper
glib::wrapper! {
    pub struct ClientRow(ObjectSubclass<imp::ClientRow>)
    @extends gtk::Box, gtk::Widget,
    @implements gtk::Accessible, gtk::Buildable, gtk::ConstraintTarget, gtk::Orientable;
}
// ANCHOR_END: glib_wrapper

impl Default for ClientRow {
    fn default() -> Self {
        Self::new()
    }
}

impl ClientRow {
    pub fn new() -> Self {
        Object::builder().build()
    }

    // ANCHOR: bind
    pub fn bind(&self, client_object: &ClientObject) {
        // Get state
        let completed_button = self.imp().completed_button.get();
        let client_id_label = self.imp().client_id_label.get();
        let mut bindings = self.imp().bindings.borrow_mut();

        // Bind `client_object.completed` to `client_row.completed_button.active`
        let completed_button_binding = client_object
            .bind_property("completed", &completed_button, "active")
            .flags(BindingFlags::SYNC_CREATE | BindingFlags::BIDIRECTIONAL)
            .build();
        // Save binding
        bindings.push(completed_button_binding);

        // Bind `client_object.client_id` to `client_row.content_label.label`
        let client_id_label_binding = client_object
            .bind_property("client_id", &client_id_label, "label")
            .flags(BindingFlags::SYNC_CREATE)
            .build();
        // Save binding
        bindings.push(client_id_label_binding);

        // Bind `client_object.completed` to `client_row.content_label.attributes`
        let content_label_binding = client_object
            .bind_property("completed", &completed_button, "attributes")
            .flags(BindingFlags::SYNC_CREATE)
            .transform_to(|_, active| {
                let attribute_list = AttrList::new();
                if active {
                    // If "active" is true, content of the label will be strikethrough
                    let attribute = AttrInt::new_strikethrough(true);
                    attribute_list.insert(attribute);
                }
                Some(attribute_list.to_value())
            })
            .build();
        // Save binding
        bindings.push(content_label_binding);
    }
    // ANCHOR_END: bind

    // ANCHOR: unbind
    pub fn unbind(&self) {
        // Unbind all stored bindings
        for binding in self.imp().bindings.borrow_mut().drain(..) {
            binding.unbind();
        }
    }
    // ANCHOR_END: unbind
}
