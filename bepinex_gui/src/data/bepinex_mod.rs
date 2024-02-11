pub struct BepInExMod {
    name: String,
    version: String,
}

impl BepInExMod {
    pub fn new(name: impl Into<String>, version: impl Into<String>) -> Self {
        Self {
            name: name.into(),
            version: version.into(),
        }
    }
pub fn copy(bepinex_mod: &BepInExMod) -> Self {
    Self {
        name: bepinex_mod.name.to_owned(),
        version: bepinex_mod.version.to_owned(),
    }
}
    pub fn name(&self) -> &str {
        self.name.as_ref()
    }
}

impl ToString for BepInExMod {
    fn to_string(&self) -> String {
        format!("{} {}", self.name, self.version)
    }
}
