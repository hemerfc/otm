use std::cell::RefCell;
use std::rc::Rc;

use glib::{ParamSpec, ParamSpecBoolean, ParamSpecInt, Value};
use gtk::glib;
use gtk::prelude::*;
use gtk::subclass::prelude::*;
use once_cell::sync::Lazy;

use super::ClientData;
use crate::client_object;

// ANCHOR: struct_and_subclass
// Object holding the state
#[derive(Default)]
pub struct ClientObject {
    pub data: Rc<RefCell<ClientData>>,
}

// The central trait for subclassing a GObject
#[glib::object_subclass]
impl ObjectSubclass for ClientObject {
    const NAME: &'static str = "TodoClientObject";
    type Type = client_object::ClientObject;
}
// ANCHOR_END: struct_and_subclass

// Trait shared by all GObjects
impl ObjectImpl for ClientObject {
    fn properties() -> &'static [ParamSpec] {
        static PROPERTIES: Lazy<Vec<ParamSpec>> = Lazy::new(|| {
            vec![
                ParamSpecBoolean::builder("completed").build(),
                ParamSpecInt::builder("client_id").build(),
            ]
        });
        PROPERTIES.as_ref()
    }

    fn set_property(&self, _id: usize, value: &Value, pspec: &ParamSpec) {
        match pspec.name() {
            "completed" => {
                let input_value = value.get().expect("The value needs to be of type `bool`.");
                self.data.borrow_mut().completed = input_value;
            }
            "client_id" => {
                let input_value = value
                    .get()
                    .expect("The value needs to be of type `i32`.");
                self.data.borrow_mut().client_id = input_value;
            }
            _ => unimplemented!(),
        }
    }

    fn property(&self, _id: usize, pspec: &ParamSpec) -> Value {
        match pspec.name() {
            "completed" => self.data.borrow().completed.to_value(),
            "client_id" => self.data.borrow().client_id.to_value(),
            _ => unimplemented!(),
        }
    }
}
