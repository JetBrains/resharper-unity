package com.jetbrains.rider.plugins.unity.spellchecker

import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml.UxmlLanguage
import com.intellij.rider.rdclient.dotnet.spellchecker.strategy.XmlBackendLanguageSpellcheckingStrategy

private class UxmlSpellcheckingStrategy : XmlBackendLanguageSpellcheckingStrategy(UxmlLanguage)
