package com.jetbrains.rider.plugins.unity.spellchecker

import com.intellij.rider.rdclient.dotnet.spellchecker.strategy.XmlBackendLanguageSpellcheckingStrategy
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml.UxmlLanguage

private class UxmlSpellcheckingStrategy : XmlBackendLanguageSpellcheckingStrategy(UxmlLanguage)
