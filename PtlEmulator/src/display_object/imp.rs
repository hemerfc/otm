use std::cell::RefCell;
use std::rc::Rc;

use glib::{ParamSpec, ParamSpecBoolean, ParamSpecString, Value};
use gtk::glib;
use gtk::prelude::*;
use gtk::subclass::prelude::*;
use once_cell::sync::Lazy;

use super::DisplayData;
use crate::display_object;

// ANCHOR: struct_and_subclass
// Object holding the state
#[derive(Default)]
pub struct DisplayObject {
    pub data: Rc<RefCell<DisplayData>>,
}

// The central trait for subclassing a GObject
#[glib::object_subclass]
impl ObjectSubclass for DisplayObject {
    const NAME: &'static str = "TodoDisplayObject";
    type Type = display_object::DisplayObject;
}
// ANCHOR_END: struct_and_subclass

// Trait shared by all GObjects
impl ObjectImpl for DisplayObject {
    fn properties() -> &'static [ParamSpec] {
        static PROPERTIES: Lazy<Vec<ParamSpec>> = Lazy::new(|| {
            vec![
                ParamSpecBoolean::builder("completed").build(),
                ParamSpecString::builder("content").build(),
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
            "content" => {
                let input_value = value
                    .get()
                    .expect("The value needs to be of type `String`.");
                self.data.borrow_mut().content = input_value;
            }
            _ => unimplemented!(),
        }
    }

    fn property(&self, _id: usize, pspec: &ParamSpec) -> Value {
        match pspec.name() {
            "completed" => self.data.borrow().completed.to_value(),
            "content" => self.data.borrow().content.to_value(),
            _ => unimplemented!(),
        }
    }
}