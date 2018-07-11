package com.jetbrains.rider.plugins.unity.restClient

class ProjectState(val islands: Array<Island>, val basedirectory: String, val assetdatabase: AssetDatabase)
class Island(val name: String, val language: String, val files: Array<String>, val defines: Array<String>, val references: Array<String>, val basedirectory: String)
class AssetDatabase(val files: Array<String>, val emptydirectories: Array<String>)
