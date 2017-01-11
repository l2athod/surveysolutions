import * as vue from "vue"

import CategoricalSingle from "./CategoricalSingle"
import DateTime from "./DateTime"
import GenericQuestion from "./GenericQuestion"
import Integer from "./Integer"
import TextQuestion from "./TextQuestion"

vue.component("DateTime", DateTime)
vue.component("Integer", Integer)
vue.component("TextQuestion", TextQuestion)
vue.component("CategoricalSingle", CategoricalSingle)
vue.component("GenericQuestion", GenericQuestion)
