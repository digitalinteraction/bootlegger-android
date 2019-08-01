#!/bin/bash
# GOOGLE_JSON_FILE=$APPCENTER_SOURCE_DIRECTORY/Bootleg.Android/google-services.json
# OURSTORY_VERSION_FILE=$APPCENTER_SOURCE_DIRECTORY/Bootleg.Android/BuildVariants/Versions/OurStory.t4

# if [ ! -n "$GOOGLE_JSON" ]
# then
#     echo "You need define the GOOGLE_JSON variable in App Center"
#     exit
# fi

# if [ ! -n "$OURSTORY_VERSION" ]
# then
#     echo "You need define the OURSTORY_VERSION variable in App Center"
#     exit
# fi

# if [ ! -e "$OURSTORY_VERSION_FILE" ]
# then
#     echo "Writing Version File"
#     mkdir -p $APPCENTER_SOURCE_DIRECTORY/Bootleg.Android/BuildVariants/Versions
#     echo "$OURSTORY_VERSION" > $OURSTORY_VERSION_FILE
#     sed -i -e 's/\\"/'\"'/g' $OURSTORY_VERSION_FILE

#     echo "File content:"
#     cat $OURSTORY_VERSION_FILE
# fi

# if [ ! -e "$GOOGLE_JSON_FILE" ]
# then
#     echo "Writing Google Json"
#     echo "$GOOGLE_JSON" > $GOOGLE_JSON_FILE
#     sed -i -e 's/\\"/'\"'/g' $GOOGLE_JSON_FILE

#     echo "File content:"
#     cat $GOOGLE_JSON_FILE
# fi

mkdir -p ${BUILD_SOURCESDIRECTORY}/Bootleg.Android/BuildVariants/Versions

echo "Link Version T4: ${AGENT_TEMPDIRECTORY}/${FLAVOUR}.t4 to ${BUILD_SOURCESDIRECTORY}/Bootleg.Android/BuildVariants/Versions/OurStory.t4"
ln -s ${AGENT_TEMPDIRECTORY}/${FLAVOUR}.t4 ${BUILD_SOURCESDIRECTORY}/Bootleg.Android/BuildVariants/Versions/OurStory.t4

echo "Link Google Json: ${AGENT_TEMPDIRECTORY}/google-services.json to ${BUILD_SOURCESDIRECTORY}/Bootleg.Android/google-services.json"
ln -s ${AGENT_TEMPDIRECTORY}/google-services.json ${BUILD_SOURCESDIRECTORY}/Bootleg.Android/google-services.json

echo "Running T4 Process"
for filename in $(find . -type f -name '*.tt')
do
    if [[ ${filename: -3} == ".tt" ]]
	then
		echo "$filename"
		mono /Applications/Visual\ Studio.app/Contents/Resources/lib/monodevelop/AddIns/MonoDevelop.TextTemplating/TextTransform.exe "$filename"		
	else
		echo WARNING: Input file not a TT: "$filename"
	fi	
done